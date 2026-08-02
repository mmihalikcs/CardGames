using System.ComponentModel;
using System.Reflection;
using CardGames.Domain.Enums;
using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;

namespace CardGames.GoFish;

internal sealed class GoFishGameManager : IGameManager
{
    private const int InitialHandSize = 7;

    private static readonly Dictionary<string, Rank> RankLookup = BuildRankLookup();

    private readonly IGameIO _Io;
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

    public GoFishGameManager(IGameIO io) : this(io, new Random())
    {
    }

    internal GoFishGameManager(IGameIO io, Random random)
    {
        _Io = io ?? throw new ArgumentNullException(nameof(io));
        _Random = random ?? throw new ArgumentNullException(nameof(random));
        _DrawPile = new DeckOfCards();
        _PlayerHand = new List<Card>();
        _ComputerHand = new List<Card>();
    }

    // Test seam: pre-seed exact hands/draw pile/book counts, bypassing shuffle nondeterminism.
    internal GoFishGameManager(
        IGameIO io, Random random,
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

        DetectAndRemoveBooks(_PlayerHand, ref _PlayerBooks, "You");
        DetectAndRemoveBooks(_ComputerHand, ref _ComputerBooks, "Computer");
    }

    private void PlayGame()
    {
        _Io.WriteLine("Welcome to Go Fish!");
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
            EnsureHandNotEmpty(_PlayerHand, "Your");
            if (_PlayerHand.Count == 0)
                break;

            DisplayHand(_PlayerHand, "Your hand");
            var rank = PromptForRank(_PlayerHand);
            again = AskForRank(rank, _PlayerHand, ref _PlayerBooks, "You", _ComputerHand, ref _ComputerBooks, "Computer");
        }
    }

    private void TakeComputerTurn()
    {
        bool again = true;
        while (again && !IsGameOver())
        {
            EnsureHandNotEmpty(_ComputerHand, "Computer's");
            if (_ComputerHand.Count == 0)
                break;

            var rank = ChooseComputerRank();
            _Io.WriteLine($"Computer asks: Do you have any {DescribeRank(rank)}s?");
            again = AskForRank(rank, _ComputerHand, ref _ComputerBooks, "Computer", _PlayerHand, ref _PlayerBooks, "You");
        }
    }

    // If a player's hand was fully drained by the opponent's prior asks but the pile still has cards,
    // they draw one card so they have something to play with on their turn.
    private void EnsureHandNotEmpty(List<Card> hand, string ownerLabel)
    {
        if (hand.Count > 0)
            return;

        var drawn = _DrawPile.DrawCard();
        if (drawn != null)
        {
            hand.Add(drawn);
            _Io.WriteLine($"{ownerLabel} hand was empty - drew a card from the pile.");
        }
    }

    // Core ask resolution. Returns true if the asker earns another turn.
    private bool AskForRank(
        Rank rank,
        List<Card> askerHand, ref int askerBooks, string askerLabel,
        List<Card> responderHand, ref int responderBooks, string responderLabel)
    {
        var matches = responderHand.Where(c => c.Rank == rank).ToList();
        if (matches.Count > 0)
        {
            foreach (var card in matches)
                responderHand.Remove(card);
            askerHand.AddRange(matches);
            _Io.WriteLine($"{responderLabel} had {matches.Count} {DescribeRank(rank)}(s)! {askerLabel} took them.");
            DetectAndRemoveBooks(askerHand, ref askerBooks, askerLabel);
            return true;
        }

        _Io.WriteLine($"{responderLabel} says: Go Fish!");
        var drawn = _DrawPile.DrawCard();
        if (drawn == null)
        {
            _Io.WriteLine("The draw pile is empty.");
            return false;
        }

        askerHand.Add(drawn);
        _Io.WriteLine($"{askerLabel} drew a card.");
        DetectAndRemoveBooks(askerHand, ref askerBooks, askerLabel);

        if (drawn.Rank == rank)
        {
            _Io.WriteLine($"{askerLabel} drew the {DescribeRank(rank)} that was asked for - go again!");
            return true;
        }

        return false;
    }

    private void DetectAndRemoveBooks(List<Card> hand, ref int bookCount, string ownerLabel)
    {
        var completedRanks = hand.GroupBy(c => c.Rank).Where(g => g.Count() == 4).Select(g => g.Key).ToList();
        foreach (var rank in completedRanks)
        {
            hand.RemoveAll(c => c.Rank == rank);
            bookCount++;
            _Io.WriteLine($"{ownerLabel} completed a book of {DescribeRank(rank)}s!");
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
        while (true)
        {
            _Io.Write("Ask for a rank (e.g. 7, J, Q, K, A): ");
            if (TryParseRank(_Io.ReadLine(), out var rank) && availableRanks.Contains(rank))
                return rank;
            _Io.WriteLine("You can only ask for a rank you currently hold. Try again.");
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

    private void DisplayHand(List<Card> hand, string label) =>
        _Io.WriteLine($"\n{label}: {string.Join(", ", hand.OrderBy(c => (int)c.Rank).Select(c => c.ToString()))}");

    private void AnnounceWinner()
    {
        _Io.WriteLine($"\nGame over! You: {_PlayerBooks} book(s), Computer: {_ComputerBooks} book(s).");
        if (_PlayerBooks > _ComputerBooks)
            _Io.WriteLine("You win!");
        else if (_ComputerBooks > _PlayerBooks)
            _Io.WriteLine("Computer wins!");
        else
            _Io.WriteLine("It's a draw!");
    }
}
