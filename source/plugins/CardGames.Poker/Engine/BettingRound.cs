using CardGames.Domain.Interaction;

namespace CardGames.Poker.Engine;

/// <summary>
/// Runs a single betting street to completion. Action order is fixed as the seat list order
/// (human first) rather than real poker's rotating order - deliberate, so rigged test seat
/// lists produce deterministic action sequences. Fixed-limit betting: fixed raise increments,
/// single pot, no side pots - an under-funded call/raise goes all-in for less and stays
/// eligible for the whole pot at showdown.
/// </summary>
internal sealed class BettingRound
{
    private readonly IReadOnlyList<Seat> _ActionOrder;
    private readonly Pot _Pot;
    private readonly IGameChannel _Io;
    private readonly AiDecisionMaker _Ai;
    private readonly Func<Seat, double> _StrengthProvider;
    private readonly int _BetIncrement;
    private readonly int _MaxRaises;

    public BettingRound(
        IReadOnlyList<Seat> actionOrder,
        Pot pot,
        IGameChannel io,
        AiDecisionMaker ai,
        Func<Seat, double> strengthProvider,
        int betIncrement = GameSettings.BetIncrement,
        int maxRaises = GameSettings.MaxRaisesPerStreet)
    {
        _ActionOrder = actionOrder;
        _Pot = pot;
        _Io = io;
        _Ai = ai;
        _StrengthProvider = strengthProvider;
        _BetIncrement = betIncrement;
        _MaxRaises = maxRaises;
    }

    /// <summary>
    /// Runs this street to completion. Returns false if the hand ended because only one seat
    /// is still in the hand (everyone else folded) - the caller must skip remaining streets/
    /// showdown and award the pot to that seat outright. Returns true otherwise, including when
    /// every remaining seat is all-in and no more action is possible.
    /// </summary>
    public bool RunStreet()
    {
        foreach (var seat in _ActionOrder.Where(s => s.IsInHand))
            seat.CommittedThisStreet = 0;

        int currentBet = 0;
        int raises = 0;
        var actedSinceLastRaise = new HashSet<Seat>();

        while (true)
        {
            if (_ActionOrder.Count(s => s.IsInHand) <= 1)
                return false;

            var actable = _ActionOrder.Where(s => s.IsActive).ToList();
            if (actable.Count == 0)
                return true;

            if (actable.All(s => actedSinceLastRaise.Contains(s) && s.CommittedThisStreet == currentBet))
                return true;

            foreach (var seat in actable)
            {
                if (!seat.IsActive)
                    continue; // may have gone all-in earlier in this same pass

                if (actedSinceLastRaise.Contains(seat) && seat.CommittedThisStreet == currentBet)
                    continue;

                int toCall = currentBet - seat.CommittedThisStreet;
                bool canRaise = raises < _MaxRaises && seat.Chips > toCall;

                var action = seat.IsHuman
                    ? PromptHuman(seat, toCall, canRaise)
                    : _Ai.Decide(_StrengthProvider(seat), toCall, seat.Chips, canRaise);

                Apply(seat, action, toCall, ref currentBet, ref raises, actedSinceLastRaise);

                if (_ActionOrder.Count(s => s.IsInHand) <= 1)
                    return false;
            }
        }
    }

    private void Apply(Seat seat, PokerAction action, int toCall, ref int currentBet, ref int raises, HashSet<Seat> actedSinceLastRaise)
    {
        switch (action)
        {
            case PokerAction.Fold:
                seat.HasFolded = true;
                _Io.Publish(new SeatFolded(seat.Name));
                actedSinceLastRaise.Add(seat);
                break;

            case PokerAction.Check:
                _Io.Publish(new SeatChecked(seat.Name));
                actedSinceLastRaise.Add(seat);
                break;

            case PokerAction.Call:
                {
                    int amount = Math.Min(toCall, seat.Chips);
                    seat.Chips -= amount;
                    seat.CommittedThisStreet += amount;
                    _Pot.Add(amount);
                    if (amount < toCall)
                        seat.IsAllIn = true;
                    _Io.Publish(new SeatCalled(seat.Name, amount, seat.IsAllIn));
                    actedSinceLastRaise.Add(seat);
                    break;
                }

            case PokerAction.Raise:
                {
                    int targetTotal = currentBet + _BetIncrement;
                    int owed = targetTotal - seat.CommittedThisStreet;
                    int amount = Math.Min(owed, seat.Chips);
                    seat.Chips -= amount;
                    seat.CommittedThisStreet += amount;
                    _Pot.Add(amount);
                    if (amount < owed)
                        seat.IsAllIn = true;
                    currentBet = seat.CommittedThisStreet;
                    raises++;
                    _Io.Publish(new SeatRaised(seat.Name, currentBet, seat.IsAllIn));
                    actedSinceLastRaise.Clear();
                    actedSinceLastRaise.Add(seat);
                    break;
                }
        }
    }

    private PokerAction PromptHuman(Seat seat, int toCall, bool canRaise)
    {
        using var scope = (_Io as ISeatContextGameChannel)?.BeginParticipantScope(seat.Name);
        while (true)
        {
            _Io.Publish(new BettingStatus(seat.Name, _Pot.Total, seat.Chips, toCall));
            var options = new List<string> { "fold", toCall == 0 ? "check" : "call" };
            if (canRaise)
                options.Add("raise");

            var response = (ChoiceResponse)_Io.Await(new ChoicePrompt(seat.Name, "Action", options));
            var input = response.OptionId.Trim().ToLowerInvariant();
            switch (input)
            {
                case "f":
                case "fold":
                    return PokerAction.Fold;
                case "ch":
                case "check":
                    if (toCall == 0) return PokerAction.Check;
                    break;
                case "c":
                case "call":
                    if (toCall > 0) return PokerAction.Call;
                    break;
                case "r":
                case "raise":
                    if (canRaise) return PokerAction.Raise;
                    break;
            }
            _Io.Publish(new InvalidActionRejected(seat.Name));
        }
    }
}
