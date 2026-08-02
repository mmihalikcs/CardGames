using CardGames.Domain.Models;

namespace CardGames.Poker.Engine;

internal static class HandEvaluator
{
    // Ace-low "wheel" straight (A-2-3-4-5), expressed as distinct descending rank values
    // (Ace=13, Two=1). The standard "5 consecutive distinct ranks" check below can't detect
    // this since 13-1 == 12, not 4, so it's special-cased explicitly.
    private static readonly int[] WheelRanks = { 13, 4, 3, 2, 1 };

    /// <summary>Scores exactly 5 cards into a comparable HandRank.</summary>
    public static HandRank EvaluateExact5(IReadOnlyList<Card> five)
    {
        if (five.Count != 5)
            throw new ArgumentException("EvaluateExact5 requires exactly 5 cards.", nameof(five));

        var ranks = five.Select(c => (int)c.Rank).ToList();
        bool isFlush = five.Select(c => c.Suit).Distinct().Count() == 1;
        var distinctDesc = ranks.Distinct().OrderByDescending(r => r).ToList();

        bool isStraight = false;
        int straightHigh = 0;
        if (distinctDesc.Count == 5 && distinctDesc[0] - distinctDesc[4] == 4)
        {
            isStraight = true;
            straightHigh = distinctDesc[0];
        }
        else if (distinctDesc.Count == 5 && distinctDesc.SequenceEqual(WheelRanks))
        {
            isStraight = true;
            straightHigh = 4; // Ace plays low; the Five is the effective high card.
        }

        var groups = ranks.GroupBy(r => r)
            .Select(g => (Rank: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ThenByDescending(g => g.Rank)
            .ToList();

        var descRanks = ranks.OrderByDescending(r => r).ToList();

        if (isFlush && isStraight)
            return new HandRank(HandCategory.StraightFlush, new[] { straightHigh });

        if (groups[0].Count == 4)
            return new HandRank(HandCategory.FourOfAKind, new[] { groups[0].Rank, groups[1].Rank });

        if (groups[0].Count == 3 && groups[1].Count == 2)
            return new HandRank(HandCategory.FullHouse, new[] { groups[0].Rank, groups[1].Rank });

        if (isFlush)
            return new HandRank(HandCategory.Flush, descRanks);

        if (isStraight)
            return new HandRank(HandCategory.Straight, new[] { straightHigh });

        if (groups[0].Count == 3)
            return new HandRank(HandCategory.ThreeOfAKind, new[] { groups[0].Rank, groups[1].Rank, groups[2].Rank });

        if (groups[0].Count == 2 && groups[1].Count == 2)
            return new HandRank(HandCategory.TwoPair, new[] { groups[0].Rank, groups[1].Rank, groups[2].Rank });

        if (groups[0].Count == 2)
            return new HandRank(HandCategory.OnePair, new[] { groups[0].Rank, groups[1].Rank, groups[2].Rank, groups[3].Rank });

        return new HandRank(HandCategory.HighCard, descRanks);
    }

    /// <summary>Best 5-card hand from any combination of the given cards (5, 6, or 7 cards in).</summary>
    public static HandRank EvaluateBest(IReadOnlyList<Card> availableCards)
    {
        if (availableCards.Count < 5)
            throw new ArgumentException("EvaluateBest requires at least 5 cards.", nameof(availableCards));

        return Combinations(availableCards, 5)
            .Select(EvaluateExact5)
            .Max();
    }

    /// <summary>
    /// Omaha's "must use exactly 2 hole + 3 community" constraint - a separate method (not a
    /// flag on EvaluateBest) since it's a structurally different rule from "pick any 5 of N".
    /// </summary>
    public static HandRank EvaluateBestOmaha(IReadOnlyList<Card> holeCards, IReadOnlyList<Card> communityCards)
    {
        if (holeCards.Count != 4)
            throw new ArgumentException("Omaha requires exactly 4 hole cards.", nameof(holeCards));
        if (communityCards.Count != 5)
            throw new ArgumentException("Omaha showdown requires exactly 5 community cards.", nameof(communityCards));

        HandRank? best = null;
        foreach (var holePair in Combinations(holeCards, 2))
        {
            foreach (var boardTriple in Combinations(communityCards, 3))
            {
                var five = holePair.Concat(boardTriple).ToList();
                var rank = EvaluateExact5(five);
                if (best is null || rank.CompareTo(best.Value) > 0)
                    best = rank;
            }
        }

        return best!.Value;
    }

    private static IEnumerable<List<T>> Combinations<T>(IReadOnlyList<T> items, int k)
    {
        if (k == 0)
        {
            yield return new List<T>();
            yield break;
        }
        if (items.Count < k)
            yield break;

        for (int i = 0; i <= items.Count - k; i++)
        {
            foreach (var rest in Combinations(items.Skip(i + 1).ToList(), k - 1))
            {
                var combo = new List<T> { items[i] };
                combo.AddRange(rest);
                yield return combo;
            }
        }
    }
}
