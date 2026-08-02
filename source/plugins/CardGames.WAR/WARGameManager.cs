using CardGames.Domain.Extensions;
using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;
using System.Text;

namespace CardGames.WAR;

internal sealed class WARGameManager : IGameManager
{
    private const int WarCardCount = 4; // 3 face-down + 1 face-up, standard rule
    private const int DefaultMaxRounds = 10_000;

    private readonly IGameIO _Io;
    private readonly int _MaxRounds;
    private Queue<Card> _PlayerHand;
    private Queue<Card> _ComputerHand;

    internal int PlayerHandCount => _PlayerHand.Count;
    internal int ComputerHandCount => _ComputerHand.Count;

    public WARGameManager(IGameIO io) : this(io, DefaultMaxRounds)
    {
    }

    private WARGameManager(IGameIO io, int maxRounds)
    {
        _Io = io ?? throw new ArgumentNullException(nameof(io));
        _MaxRounds = maxRounds;
        _PlayerHand = new Queue<Card>();
        _ComputerHand = new Queue<Card>();
    }

    // Test seam: lets tests rig exact hands and shrink the round cap.
    internal WARGameManager(IGameIO io, Queue<Card> playerHand, Queue<Card> computerHand, int maxRounds = DefaultMaxRounds)
        : this(io, maxRounds)
    {
        _PlayerHand = playerHand;
        _ComputerHand = computerHand;
    }

    public void StartGame()
    {
        if (_PlayerHand.Count == 0 && _ComputerHand.Count == 0)
            DealHands();
        PlayRounds();
    }

    private void DealHands()
    {
        var deck = new DeckOfCards();
        deck.InitializeStandardDeck(includeJokers: false);
        deck.ShuffleDeck();

        _PlayerHand = new Queue<Card>();
        _ComputerHand = new Queue<Card>();
        bool dealToPlayer = true;
        Card? card;
        while ((card = deck.DrawCard()) != null)
        {
            if (dealToPlayer)
                _PlayerHand.Enqueue(card);
            else
                _ComputerHand.Enqueue(card);
            dealToPlayer = !dealToPlayer;
        }
    }

    private void PlayRounds()
    {
        _Io.WriteLine("Welcome to WAR! Press Enter after each round to continue.");
        int round = 0;
        while (_PlayerHand.Count > 0 && _ComputerHand.Count > 0 && round < _MaxRounds)
        {
            round++;
            _Io.Write("\nPress Enter to flip your next cards...");
            _Io.ReadLine();

            var pile = new List<Card>();
            var playerCard = _PlayerHand.Dequeue();
            var computerCard = _ComputerHand.Dequeue();
            pile.Add(playerCard);
            pile.Add(computerCard);
            _Io.WriteLine($"You played {playerCard}. Computer played {computerCard}.");
            _Io.WriteLine(RenderVersus(playerCard, "You", computerCard, "Computer"));

            int cmp = ((int)playerCard.Rank).CompareTo((int)computerCard.Rank);
            if (cmp > 0)
                AwardPile(_PlayerHand, pile, "You");
            else if (cmp < 0)
                AwardPile(_ComputerHand, pile, "Computer");
            else
            {
                _Io.WriteLine("War!");
                ResolveWar(pile);
            }
        }

        AnnounceResult(round);
    }

    private void AwardPile(Queue<Card> winnerHand, List<Card> pile, string winnerLabel)
    {
        _Io.WriteLine($"{winnerLabel} won the pile of {pile.Count} card(s).");
        foreach (var card in pile)
            winnerHand.Enqueue(card);
    }

    private void ResolveWar(List<Card> pile)
    {
        while (true)
        {
            var (playerCards, playerFaceUp) = CommitWarCards(_PlayerHand, WarCardCount);
            var (computerCards, computerFaceUp) = CommitWarCards(_ComputerHand, WarCardCount);
            pile.AddRange(playerCards);
            pile.AddRange(computerCards);

            if (playerFaceUp is null && computerFaceUp is null)
            {
                _Io.WriteLine("Both players ran out of cards at the same time - the game ends in a draw.");
                return;
            }
            if (playerFaceUp is null)
            {
                _Io.WriteLine("You ran out of cards during the war and lose the game!");
                AwardPile(_ComputerHand, pile, "Computer");
                return;
            }
            if (computerFaceUp is null)
            {
                _Io.WriteLine("Computer ran out of cards during the war and loses the game!");
                AwardPile(_PlayerHand, pile, "You");
                return;
            }

            _Io.WriteLine($"War cards: You played {playerFaceUp}. Computer played {computerFaceUp}.");
            _Io.WriteLine(RenderVersus(playerFaceUp, "You", computerFaceUp, "Computer"));

            int cmp = ((int)playerFaceUp.Rank).CompareTo((int)computerFaceUp.Rank);
            if (cmp > 0)
            {
                AwardPile(_PlayerHand, pile, "You");
                return;
            }
            if (cmp < 0)
            {
                AwardPile(_ComputerHand, pile, "Computer");
                return;
            }

            _Io.WriteLine("War again!");
        }
    }

    // Renders two cards side-by-side (e.g. "You" vs. "Computer") for a visual round display.
    private static string RenderVersus(Card left, string leftLabel, Card right, string rightLabel)
    {
        var leftLines = left.DisplayCard().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var rightLines = right.DisplayCard().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        var sb = new StringBuilder();
        sb.AppendLine($"{leftLabel.PadRight(leftLines[0].Length)}   {rightLabel}");
        for (int i = 0; i < leftLines.Length; i++)
            sb.AppendLine($"{leftLines[i]}   {rightLines[i]}");
        return sb.ToString();
    }

    // Commits up to `count` cards from hand; if fewer remain, commits all of them.
    // A null face-up card means the hand was already empty - that side loses the war.
    private static (List<Card> committed, Card? faceUp) CommitWarCards(Queue<Card> hand, int count)
    {
        var committed = new List<Card>();
        for (int i = 0; i < count && hand.Count > 0; i++)
            committed.Add(hand.Dequeue());
        return (committed, committed.Count > 0 ? committed[^1] : null);
    }

    private void AnnounceResult(int roundsPlayed)
    {
        if (_PlayerHand.Count > 0 && _ComputerHand.Count > 0 && roundsPlayed >= _MaxRounds)
        {
            _Io.WriteLine($"\nRound limit ({_MaxRounds}) reached without a natural winner.");
            if (_PlayerHand.Count > _ComputerHand.Count)
                _Io.WriteLine("You win on card count!");
            else if (_ComputerHand.Count > _PlayerHand.Count)
                _Io.WriteLine("Computer wins on card count!");
            else
                _Io.WriteLine("It's a draw!");
            return;
        }

        if (_PlayerHand.Count == 0 && _ComputerHand.Count == 0)
        {
            _Io.WriteLine("\nThe game ended in a draw - both players ran out of cards at once.");
            return;
        }

        if (_PlayerHand.Count == 0)
        {
            _Io.WriteLine("\nComputer wins! You ran out of cards.");
            return;
        }

        _Io.WriteLine("\nYou win! Computer ran out of cards.");
    }
}
