using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;
using CardGames.Poker.Engine;

namespace CardGames.Poker;

internal sealed class FiveCardDrawGameManager : IGameManager
{
    private const int HandSize = 5;

    private readonly IGameIO _Io;
    private readonly Random _Random;
    private readonly int _MaxHands;
    private readonly List<Seat> _Seats;
    private PokerDeck? _PresetDeck;

    public FiveCardDrawGameManager(IGameIO io)
    {
        _Io = io ?? throw new ArgumentNullException(nameof(io));
        _Random = new Random();
        _Seats = new List<Seat>();
        _PresetDeck = null;
        _MaxHands = GameSettings.DefaultMaxHands;
    }

    // Test seam: rig seats/deck/random and cap hands played.
    internal FiveCardDrawGameManager(IGameIO io, Random random, List<Seat> seats, PokerDeck deck, int maxHands = 1)
    {
        _Io = io ?? throw new ArgumentNullException(nameof(io));
        _Random = random ?? throw new ArgumentNullException(nameof(random));
        _Seats = seats ?? throw new ArgumentNullException(nameof(seats));
        _PresetDeck = deck;
        _MaxHands = maxHands;
    }

    internal IReadOnlyList<Seat> Seats => _Seats;

    public void StartGame()
    {
        if (_Seats.Count == 0)
            SetupSeats();

        int handNumber = 0;
        while (_Seats.Count(s => s.Chips > 0) > 1 && handNumber < _MaxHands)
        {
            handNumber++;
            PlayHand(handNumber);
        }

        AnnounceSessionResult();
    }

    private void SetupSeats()
    {
        int count = GameSettings.MinOpponents;
        while (true)
        {
            _Io.Write($"How many AI opponents ({GameSettings.MinOpponents}-{GameSettings.MaxOpponents})? ");
            if (int.TryParse(_Io.ReadLine(), out var parsed) && parsed >= GameSettings.MinOpponents && parsed <= GameSettings.MaxOpponents)
            {
                count = parsed;
                break;
            }
            _Io.WriteLine("Invalid entry. Try again.");
        }

        _Seats.Add(new Seat("You", isHuman: true, GameSettings.StartingChips));
        for (int i = 1; i <= count; i++)
            _Seats.Add(new Seat($"AI {i}", isHuman: false, GameSettings.StartingChips));
    }

    private void PlayHand(int handNumber)
    {
        _Io.WriteLine();
        _Io.WriteLine("=== New Hand ===");
        foreach (var seat in _Seats)
        {
            seat.HoleCards.Clear();
            seat.CommittedThisStreet = 0;
            seat.IsAllIn = false;
            seat.HasFolded = seat.Chips <= 0; // busted seats sit out
        }

        var deck = handNumber == 1 && _PresetDeck != null ? _PresetDeck : new PokerDeck(_Random);
        _PresetDeck = null;

        var pot = new Pot();
        PostAntes(pot);

        for (int i = 0; i < HandSize; i++)
            foreach (var seat in _Seats.Where(s => s.IsInHand))
                seat.HoleCards.Add(deck.Draw());

        TableRenderer.ShowStacks(_Io, _Seats);
        TableRenderer.ShowHoleCards(_Io, _Seats.First(s => s.IsHuman));

        var ai = new AiDecisionMaker(_Random);

        if (!RunStreet(pot, ai)) { AwardUncontested(pot); return; }

        RunDrawPhase(deck);
        TableRenderer.ShowHoleCards(_Io, _Seats.First(s => s.IsHuman));

        if (!RunStreet(pot, ai)) { AwardUncontested(pot); return; }

        var contenders = _Seats.Where(s => s.IsInHand).ToList();
        ShowdownResolver.Resolve(contenders, seat => HandEvaluator.EvaluateExact5(seat.HoleCards), pot, _Io);
    }

    private void PostAntes(Pot pot)
    {
        foreach (var seat in _Seats.Where(s => s.IsInHand))
        {
            int amount = Math.Min(GameSettings.AnteAmount, seat.Chips);
            seat.Chips -= amount;
            pot.Add(amount);
            if (seat.Chips == 0)
                seat.IsAllIn = true;
        }
    }

    private bool RunStreet(Pot pot, AiDecisionMaker ai)
    {
        var round = new BettingRound(_Seats, pot, _Io, ai,
            seat => AiDecisionMaker.StrengthFromCategory(HandEvaluator.EvaluateExact5(seat.HoleCards).Category));
        return round.RunStreet();
    }

    private void RunDrawPhase(PokerDeck deck)
    {
        _Io.WriteLine();
        _Io.WriteLine("--- Draw Phase ---");
        foreach (var seat in _Seats.Where(s => s.IsInHand))
        {
            var discardIndices = seat.IsHuman ? PromptHumanDiscards(seat) : ChooseAiDiscardIndices(seat.HoleCards);
            if (discardIndices.Count == 0)
            {
                _Io.WriteLine($"{seat.Name} stands pat.");
                continue;
            }

            foreach (var index in discardIndices.OrderByDescending(i => i))
                seat.HoleCards.RemoveAt(index);

            for (int i = 0; i < discardIndices.Count; i++)
                seat.HoleCards.Add(deck.Draw());

            _Io.WriteLine($"{seat.Name} draws {discardIndices.Count} card(s).");
        }
    }

    private List<int> PromptHumanDiscards(Seat seat)
    {
        _Io.WriteLine();
        TableRenderer.ShowHoleCards(_Io, seat);
        _Io.Write("Enter positions to discard (1-5, space separated, blank to keep all): ");
        var input = _Io.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            return new List<int>();

        var indices = new List<int>();
        foreach (var token in input.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(token, out var position) && position >= 1 && position <= seat.HoleCards.Count && !indices.Contains(position - 1))
                indices.Add(position - 1);
        }
        return indices;
    }

    // Simple strategy: stand pat on a made straight or better; otherwise keep paired/grouped
    // ranks (trips/two pair/pair) and discard the rest; with no pair, keep only the highest card.
    private static List<int> ChooseAiDiscardIndices(List<Card> hand)
    {
        var category = HandEvaluator.EvaluateExact5(hand).Category;
        if (category >= HandCategory.Straight)
            return new List<int>();

        var rankOf = hand.Select(c => (int)c.Rank).ToList();
        var keepRanks = rankOf.GroupBy(r => r).Where(g => g.Count() >= 2).Select(g => g.Key).ToHashSet();

        if (keepRanks.Count == 0)
        {
            int highestIndex = rankOf.IndexOf(rankOf.Max());
            return Enumerable.Range(0, hand.Count).Where(i => i != highestIndex).ToList();
        }

        return Enumerable.Range(0, hand.Count).Where(i => !keepRanks.Contains(rankOf[i])).ToList();
    }

    private void AwardUncontested(Pot pot)
    {
        var winner = _Seats.Single(s => s.IsInHand);
        winner.Chips += pot.Total;
        _Io.WriteLine($"{winner.Name} wins the pot of {pot.Total} uncontested!");
    }

    private void AnnounceSessionResult()
    {
        var remaining = _Seats.Where(s => s.Chips > 0).ToList();
        _Io.WriteLine(remaining.Count == 1
            ? $"\n{remaining[0].Name} wins the session!"
            : "\nSession ended.");
    }
}
