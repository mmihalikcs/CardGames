using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;

namespace CardGames.Poker.Engine;

/// <summary>
/// Shared session/hand loop for the two community-card variants (Texas Hold'em, Omaha), which
/// differ only in hole card count and the showdown evaluation rule. Five-Card Draw has no
/// community cards and a draw phase, so it's structurally different enough to stay separate.
/// </summary>
internal abstract class CommunityCardGameManagerBase : IGameManager
{
    protected readonly IGameIO Io;
    private readonly Random _Random;
    private readonly int _MaxHands;
    private readonly List<Seat> _Seats;
    private PokerDeck? _PresetDeck;

    protected abstract int HoleCardCount { get; }

    protected abstract HandRank EvaluateShowdown(Seat seat, IReadOnlyList<Card> community);

    protected CommunityCardGameManagerBase(IGameIO io)
        : this(io, new Random(), new List<Seat>(), null, GameSettings.DefaultMaxHands)
    {
    }

    // Test seam: rig seats/deck/random and cap hands played (typically 1 per test).
    protected CommunityCardGameManagerBase(IGameIO io, Random random, List<Seat> seats, PokerDeck? deck, int maxHands)
    {
        Io = io ?? throw new ArgumentNullException(nameof(io));
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

    private void SetupSeats() => SeatSetup.BuildSeats(Io, _Seats);

    private void PlayHand(int handNumber)
    {
        Io.WriteLine();
        Io.WriteLine("=== New Hand ===");
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

        for (int i = 0; i < HoleCardCount; i++)
            foreach (var seat in _Seats.Where(s => s.IsInHand))
                seat.HoleCards.Add(deck.Draw());

        TableRenderer.ShowStacks(Io, _Seats);
        ShowHoleCardsToEachHuman();

        var community = new List<Card>();
        var ai = new AiDecisionMaker(_Random);

        if (!RunStreet(pot, ai, community)) { AwardUncontested(pot); return; }

        DealCommunity(deck, community, 3); // flop
        TableRenderer.ShowCommunityCards(Io, community);
        ShowHoleCardsToEachHuman();
        if (!RunStreet(pot, ai, community)) { AwardUncontested(pot); return; }

        DealCommunity(deck, community, 1); // turn
        TableRenderer.ShowCommunityCards(Io, community);
        ShowHoleCardsToEachHuman();
        if (!RunStreet(pot, ai, community)) { AwardUncontested(pot); return; }

        DealCommunity(deck, community, 1); // river
        TableRenderer.ShowCommunityCards(Io, community);
        ShowHoleCardsToEachHuman();
        if (!RunStreet(pot, ai, community)) { AwardUncontested(pot); return; }

        var contenders = _Seats.Where(s => s.IsInHand).ToList();
        ShowdownResolver.Resolve(contenders, seat => EvaluateShowdown(seat, community), pot, Io);
    }

    private void ShowHoleCardsToEachHuman()
    {
        foreach (var seat in _Seats.Where(s => s.IsHuman))
        {
            using var scope = (Io as ISeatContextGameIO)?.BeginParticipantScope(seat.Name);
            TableRenderer.ShowHoleCards(Io, seat);
        }
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

    private bool RunStreet(Pot pot, AiDecisionMaker ai, List<Card> community)
    {
        var round = new BettingRound(_Seats, pot, Io, ai, seat => EstimateStrength(seat, community));
        return round.RunStreet();
    }

    private double EstimateStrength(Seat seat, IReadOnlyList<Card> community)
    {
        if (community.Count == 0)
            return PreflopHeuristic.Estimate(seat.HoleCards);

        var combined = seat.HoleCards.Concat(community).ToList();
        var rank = HandEvaluator.EvaluateBest(combined);
        return AiDecisionMaker.StrengthFromCategory(rank.Category);
    }

    private void AwardUncontested(Pot pot)
    {
        var winner = _Seats.Single(s => s.IsInHand);
        winner.Chips += pot.Total;
        Io.WriteLine($"{winner.Name} wins the pot of {pot.Total} uncontested!");
    }

    private static void DealCommunity(PokerDeck deck, List<Card> community, int count)
    {
        for (int i = 0; i < count; i++)
            community.Add(deck.Draw());
    }

    private void AnnounceSessionResult()
    {
        var remaining = _Seats.Where(s => s.Chips > 0).ToList();
        Io.WriteLine(remaining.Count == 1
            ? $"\n{remaining[0].Name} wins the session!"
            : "\nSession ended.");
    }
}
