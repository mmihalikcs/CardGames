namespace CardGames.Poker.Engine;

/// <summary>
/// Simple hand-strength-based AI: no pot-odds math, no bluffing (never raises below
/// RaiseThreshold). Strength is a 0-1 scale from StrengthFromCategory or PreflopHeuristic.
/// </summary>
internal sealed class AiDecisionMaker
{
    private const double FoldThreshold = 0.20;
    private const double RaiseThreshold = 0.55;
    private const double RaiseChance = 0.5;
    private const double LooseCallChance = 0.35;

    private readonly Random _Random;

    public AiDecisionMaker(Random random)
    {
        _Random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public static double StrengthFromCategory(HandCategory category) => category switch
    {
        HandCategory.HighCard => 0.05,
        HandCategory.OnePair => 0.20,
        HandCategory.TwoPair => 0.35,
        HandCategory.ThreeOfAKind => 0.50,
        HandCategory.Straight => 0.65,
        HandCategory.Flush => 0.75,
        HandCategory.FullHouse => 0.85,
        HandCategory.FourOfAKind => 0.95,
        HandCategory.StraightFlush => 1.0,
        _ => 0.05,
    };

    public PokerAction Decide(double strength, int toCall, int chipsRemaining, bool canRaise)
    {
        if (toCall == 0)
            return canRaise && strength >= RaiseThreshold && _Random.NextDouble() < RaiseChance
                ? PokerAction.Raise
                : PokerAction.Check;

        if (canRaise && strength >= RaiseThreshold && _Random.NextDouble() < RaiseChance)
            return PokerAction.Raise;

        if (strength < FoldThreshold && _Random.NextDouble() > LooseCallChance)
            return PokerAction.Fold;

        // BettingRound caps/all-ins this if chipsRemaining < toCall.
        return PokerAction.Call;
    }
}
