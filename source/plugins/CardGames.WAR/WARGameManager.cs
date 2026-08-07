using CardGames.Domain.Interaction;
using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;

namespace CardGames.WAR;

internal sealed class WARGameManager : IGameManager
{
    private const int WarCardCount = 4; // 3 face-down + 1 face-up, standard rule
    private const int DefaultMaxRounds = 10_000;

    private readonly IGameChannel _Io;
    private readonly int _MaxRounds;
    private Queue<Card> _PlayerHand;
    private Queue<Card> _ComputerHand;

    internal int PlayerHandCount => _PlayerHand.Count;
    internal int ComputerHandCount => _ComputerHand.Count;

    public WARGameManager(IGameChannel io) : this(io, DefaultMaxRounds)
    {
    }

    private WARGameManager(IGameChannel io, int maxRounds)
    {
        _Io = io ?? throw new ArgumentNullException(nameof(io));
        _MaxRounds = maxRounds;
        _PlayerHand = new Queue<Card>();
        _ComputerHand = new Queue<Card>();
    }

    // Test seam: lets tests rig exact hands and shrink the round cap.
    internal WARGameManager(IGameChannel io, Queue<Card> playerHand, Queue<Card> computerHand, int maxRounds = DefaultMaxRounds)
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
        _Io.Publish(new GameStarted());
        int round = 0;
        while (_PlayerHand.Count > 0 && _ComputerHand.Count > 0 && round < _MaxRounds)
        {
            round++;
            _Io.Await(new ConfirmPrompt(Seats.Player, "\nPress Enter to flip your next cards..."));

            var pile = new List<Card>();
            var playerCard = _PlayerHand.Dequeue();
            var computerCard = _ComputerHand.Dequeue();
            pile.Add(playerCard);
            pile.Add(computerCard);
            _Io.Publish(new CardsRevealed(playerCard, computerCard, Reason: "Flip"));

            int cmp = ((int)playerCard.Rank).CompareTo((int)computerCard.Rank);
            if (cmp > 0)
                AwardPile(_PlayerHand, pile, Seats.Player);
            else if (cmp < 0)
                AwardPile(_ComputerHand, pile, Seats.Computer);
            else
            {
                _Io.Publish(new WarTriggered());
                if (!ResolveWar(pile))
                    return; // ended the game (mutual/one-sided run-out during war)
            }
        }

        AnnounceResult(round);
    }

    private void AwardPile(Queue<Card> winnerHand, List<Card> pile, string winnerSeatId)
    {
        _Io.Publish(new PileAwarded(winnerSeatId, pile.Count));
        foreach (var card in pile)
            winnerHand.Enqueue(card);
    }

    /// <returns>false if the war ended the game outright (a hand ran out mid-war).</returns>
    private bool ResolveWar(List<Card> pile)
    {
        while (true)
        {
            var (playerCards, playerFaceUp) = CommitWarCards(_PlayerHand, WarCardCount);
            var (computerCards, computerFaceUp) = CommitWarCards(_ComputerHand, WarCardCount);
            pile.AddRange(playerCards);
            pile.AddRange(computerCards);

            if (playerFaceUp is null && computerFaceUp is null)
            {
                _Io.Publish(new GameEnded(WinnerSeatId: null, Reason: "MutualRunOutDuringWar"));
                return false;
            }
            if (playerFaceUp is null)
            {
                _Io.Publish(new GameEnded(Seats.Computer, Reason: "OpponentRanOutDuringWar"));
                AwardPile(_ComputerHand, pile, Seats.Computer);
                return false;
            }
            if (computerFaceUp is null)
            {
                _Io.Publish(new GameEnded(Seats.Player, Reason: "OpponentRanOutDuringWar"));
                AwardPile(_PlayerHand, pile, Seats.Player);
                return false;
            }

            _Io.Publish(new CardsRevealed(playerFaceUp, computerFaceUp, Reason: "War"));

            int cmp = ((int)playerFaceUp.Rank).CompareTo((int)computerFaceUp.Rank);
            if (cmp > 0)
            {
                AwardPile(_PlayerHand, pile, Seats.Player);
                return true;
            }
            if (cmp < 0)
            {
                AwardPile(_ComputerHand, pile, Seats.Computer);
                return true;
            }

            _Io.Publish(new WarTriggered(Repeat: true));
        }
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
            _Io.Publish(new RoundLimitReached(_MaxRounds));
            var leader = _PlayerHand.Count > _ComputerHand.Count ? Seats.Player
                : _ComputerHand.Count > _PlayerHand.Count ? Seats.Computer
                : (string?)null;
            _Io.Publish(new GameEnded(leader, Reason: "RoundLimitReached"));
            return;
        }

        if (_PlayerHand.Count == 0 && _ComputerHand.Count == 0)
        {
            _Io.Publish(new GameEnded(null, Reason: "MutualRunOut"));
            return;
        }

        if (_PlayerHand.Count == 0)
        {
            _Io.Publish(new GameEnded(Seats.Computer, Reason: "RanOutOfCards"));
            return;
        }

        _Io.Publish(new GameEnded(Seats.Player, Reason: "RanOutOfCards"));
    }
}
