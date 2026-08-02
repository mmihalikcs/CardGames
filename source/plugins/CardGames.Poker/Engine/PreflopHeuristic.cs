using CardGames.Domain.Models;

namespace CardGames.Poker.Engine;

/// <summary>
/// Hand-strength estimate for streets with fewer than 5 known cards (Hold'em/Omaha preflop),
/// where HandEvaluator can't run. Five-Card Draw always has exactly 5 known cards and never
/// needs this - it uses HandEvaluator.EvaluateExact5 + AiDecisionMaker.StrengthFromCategory.
/// </summary>
internal static class PreflopHeuristic
{
    public static double Estimate(IReadOnlyList<Card> holeCards)
    {
        var ranks = holeCards.Select(c => (int)c.Rank).ToList();
        double score = ranks.Max() / 13.0 * 0.4;

        var pairRanks = ranks.GroupBy(r => r).Where(g => g.Count() >= 2).ToList();
        if (pairRanks.Count > 0)
            score += 0.3 + pairRanks.Max(g => g.Key) / 13.0 * 0.2;

        if (holeCards.Select(c => c.Suit).Distinct().Count() == 1)
            score += 0.05;

        return Math.Clamp(score, 0.0, 1.0);
    }
}
