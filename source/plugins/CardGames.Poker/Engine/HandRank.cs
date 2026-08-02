namespace CardGames.Poker.Engine;

/// <summary>
/// A comparable poker hand score: category first, then tiebreaker ranks (1-13, Ace=13) in
/// order of significance. No == / &gt; / &lt; operator overloads - callers use CompareTo() directly.
/// </summary>
internal readonly struct HandRank : IComparable<HandRank>
{
    public HandCategory Category { get; }

    public IReadOnlyList<int> Tiebreakers { get; }

    public HandRank(HandCategory category, IReadOnlyList<int> tiebreakers)
    {
        Category = category;
        Tiebreakers = tiebreakers;
    }

    public int CompareTo(HandRank other)
    {
        int categoryComparison = Category.CompareTo(other.Category);
        if (categoryComparison != 0)
            return categoryComparison;

        int count = Math.Min(Tiebreakers.Count, other.Tiebreakers.Count);
        for (int i = 0; i < count; i++)
        {
            int comparison = Tiebreakers[i].CompareTo(other.Tiebreakers[i]);
            if (comparison != 0)
                return comparison;
        }
        return 0;
    }
}
