using System.ComponentModel;
using System.Reflection;
using System.Text;
using CardGames.Domain.Enums;
using CardGames.Domain.Extensions;
using CardGames.Domain.Interaction;
using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;

namespace CardGames.GoFish;

internal sealed class GoFishGameManager : IGameManager
{
    private const int InitialHandSize = 7;

    private static readonly Dictionary<string, Rank> RankLookup = BuildRankLookup();

    private readonly IGameChannel _Io;
    private readonly Random _Random;
    private DeckOfCards _DrawPile;
    private List<Card> _PlayerHand;
    private List<Card> _ComputerHand;
    private int _PlayerBooks;
    private int _ComputerBooks;

    internal int PlayerBookCount => _PlayerBooks;
    internal int ComputerBookCount => _ComputerBooks;
    internal int PlayerHandCount => _PlayerHand.Count;
    internal int ComputerHandCount => _ComputerHand.Count;
    internal int DrawPileCount => _DrawPile.CurrentDeck.Count;

    public GoFishGameManager(IGameChannel io) : this(io, new Random())
    {
    }

    internal GoFishGameManager(IGameChannel io, Random random)
    {
        _Io = io ?? throw new ArgumentNullException(nameof(io));
        _Random = random ?? throw new ArgumentNullException(nameof(random));
        _DrawPile = new DeckOfCards();
        _PlayerHand = new List<Card>();
        _ComputerHand = new List<Card>();
    }

    // Test seam: pre-seed exact hands/draw pile/book counts, bypassing shuffle nondeterminism.
    internal GoFishGameManager(
        IGameChannel io, Random random,
        List<Card> playerHand, List<Card> computerHand, DeckOfCards drawPile,
        int playerBooks = 0, int computerBooks = 0)
        : this(io, random)
    {
        _PlayerHand = playerHand;
        _ComputerHand = computerHand;
        _DrawPile = drawPile;
        _PlayerBooks = playerBooks;
        _ComputerBooks = computerBooks;
    }

    public void StartGame()
    {
        if (_PlayerHand.Count == 0 && _ComputerHand.Count == 0 && _DrawPile.CurrentDeck.Count == 0)
            DealHands();
        PlayGame();
    }

    private void DealHands()
    {
        var deck = new DeckOfCards();
        deck.InitializeStandardDeck(includeJokers: false);
        deck.ShuffleDeck();

        _PlayerHand = new List<Card>();
        _ComputerHand = new List<Card>();
        for (int i = 0; i < InitialHandSize; i++)
        {
            _PlayerHand.Add(deck.DrawCard()!);
            _ComputerHand.Add(deck.DrawCard()!);
        }
        _DrawPile = deck;

        DetectAndRemoveBooks(_PlayerHand, ref _PlayerBooks, Seats.Player);
        DetectAndRemoveBooks(_ComputerHand, ref _ComputerBooks, Seats.Computer);
    }

    private void PlayGame()
    {
        _Io.Publish(new GameStarted());
        bool isPlayerTurn = true;
        while (!IsGameOver())
        {
            if (isPlayerTurn)
                TakePlayerTurn();
            else
                TakeComputerTurn();
            isPlayerTurn = !isPlayerTurn;
        }

        AnnounceWinner();
    }

    private bool IsGameOver() =>
        _DrawPile.CurrentDeck.Count == 0 && (_PlayerHand.Count == 0 || _ComputerHand.Count == 0);

    private void TakePlayerTurn()
    {
        bool again = true;
        while (again && !IsGameOver())
        {
            EnsureHandNotEmpty(_PlayerHand, Seats.Player);
            if (_PlayerHand.Count == 0)
                break;

            DisplayHand(_PlayerHand, Seats.Player);
            var rank = PromptForRank(_PlayerHand);
            again = AskForRank(rank, _PlayerHand, ref _PlayerBooks, Seats.Player, _ComputerHand, ref _ComputerBooks, Seats.Computer);
        }
    }

    private void TakeComputerTurn()
    {
        bool again = true;
        while (again && !IsGameOver())
        {
            EnsureHandNotEmpty(_ComputerHand, Seats.Computer);
            if (_ComputerHand.Count == 0)
                break;

            var rank = ChooseComputerRank();
            _Io.Publish(new ComputerAnnouncedAsk(DescribeRank(rank)));
            again = AskForRank(rank, _ComputerHand, ref _ComputerBooks, Seats.Computer, _PlayerHand, ref _PlayerBooks, Seats.Player);
        }
    }

    // If a player's hand was fully drained by the opponent's prior asks but the pile still has cards,
    // they draw one card so they have something to play with on their turn.
    private void EnsureHandNotEmpty(List<Card> hand, string seatId)
    {
        if (hand.Count > 0)
            return;

        var drawn = _DrawPile.DrawCard();
        if (drawn != null)
        {
            hand.Add(drawn);
            _Io.Publish(new HandReplenished(seatId));
        }
    }

    // Core ask resolution. Returns true if the asker earns another turn.
    private bool AskForRank(
        Rank rank,
        List<Card> askerHand, ref int askerBooks, string askerSeatId,
        List<Card> responderHand, ref int responderBooks, string responderSeatId)
    {
        var matches = responderHand.Where(c => c.Rank == rank).ToList();
        if (matches.Count > 0)
        {
            foreach (var card in matches)
                responderHand.Remove(card);
            askerHand.AddRange(matches);
            _Io.Publish(new RankMatched(askerSeatId, responderSeatId, DescribeRank(rank), matches.Count));
            DetectAndRemoveBooks(askerHand, ref askerBooks, askerSeatId);
            return true;
        }

        _Io.Publish(new GoFishCalled(responderSeatId));
        var drawn = _DrawPile.DrawCard();
        if (drawn == null)
        {
            _Io.Publish(new DrawPileEmpty());
            return false;
        }

        askerHand.Add(drawn);
        _Io.Publish(new CardDrawn(askerSeatId));
        DetectAndRemoveBooks(askerHand, ref askerBooks, askerSeatId);

        if (drawn.Rank == rank)
        {
            _Io.Publish(new DrewAskedRank(askerSeatId, DescribeRank(rank)));
            return true;
        }

        return false;
    }

    private void DetectAndRemoveBooks(List<Card> hand, ref int bookCount, string seatId)
    {
        var completedRanks = hand.GroupBy(c => c.Rank).Where(g => g.Count() == 4).Select(g => g.Key).ToList();
        foreach (var rank in completedRanks)
        {
            hand.RemoveAll(c => c.Rank == rank);
            bookCount++;
            _Io.Publish(new BookCompleted(seatId, DescribeRank(rank)));
        }
    }

    private Rank ChooseComputerRank()
    {
        var heldRanks = _ComputerHand.Select(c => c.Rank).Distinct().ToList();
        return heldRanks[_Random.Next(heldRanks.Count)];
    }

    private Rank PromptForRank(List<Card> hand)
    {
        var availableRanks = hand.Select(c => c.Rank).Distinct().ToHashSet();
        var validOptionsHint = availableRanks.Select(DescribeRank).ToList();
        while (true)
        {
            var response = (ChoiceResponse)_Io.Await(new ChoicePrompt(Seats.Player, "Ask for a rank", validOptionsHint));
            if (TryParseRank(response.OptionId, out var rank) && availableRanks.Contains(rank))
                return rank;
            _Io.Publish(new RankAskRejected(Seats.Player, "You can only ask for a rank you currently hold. Try again."));
        }
    }

    private static bool TryParseRank(string? input, out Rank rank)
    {
        rank = default;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var key = input.Trim().ToUpperInvariant();
        if (RankLookup.TryGetValue(key, out rank))
            return true;

        return Enum.TryParse(input.Trim(), ignoreCase: true, out rank) && rank != Rank.None && rank != Rank.Joker;
    }

    private static string DescribeRank(Rank rank) =>
        typeof(Rank).GetField(rank.ToString())!.GetCustomAttribute<DescriptionAttribute>()!.Description;

    private static Dictionary<string, Rank> BuildRankLookup() =>
        Enum.GetValues<Rank>()
            .Where(r => r != Rank.None && r != Rank.Joker)
            .ToDictionary(r => DescribeRank(r).ToUpperInvariant(), r => r);

    private void DisplayHand(List<Card> hand, string seatId) =>
        _Io.Publish(new HandDisplayed(seatId, RenderHandRow(hand.OrderBy(c => (int)c.Rank).ToList())));

    // Renders a hand as a row of graphical cards, matching WAR's/Poker's DisplayCard()-based rendering.
    private static string RenderHandRow(IReadOnlyList<Card> hand)
    {
        if (hand.Count == 0)
            return string.Empty;

        var cardLines = hand.Select(c => c.DisplayCard().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)).ToList();
        int lineCount = cardLines[0].Length;

        var sb = new StringBuilder();
        for (int line = 0; line < lineCount; line++)
            sb.AppendLine(string.Join(" ", cardLines.Select(lines => lines[line])));
        return sb.ToString();
    }

    private void AnnounceWinner()
    {
        var winnerSeatId = _PlayerBooks > _ComputerBooks ? Seats.Player
            : _ComputerBooks > _PlayerBooks ? Seats.Computer
            : (string?)null;
        _Io.Publish(new GameEnded(_PlayerBooks, _ComputerBooks, winnerSeatId));
    }
}
