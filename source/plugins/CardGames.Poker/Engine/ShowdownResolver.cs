using CardGames.Domain.Interfaces;

namespace CardGames.Poker.Engine;

internal static class ShowdownResolver
{
    public static void Resolve(IReadOnlyList<Seat> seatsInHand, Func<Seat, HandRank> evaluator, Pot pot, IGameIO io)
    {
        var ranked = seatsInHand.Select(s => (Seat: s, Rank: evaluator(s))).ToList();
        foreach (var (seat, rank) in ranked)
            io.WriteLine($"{seat.Name} shows {Describe(rank)}.");

        var best = ranked.Select(r => r.Rank).Max();
        var winners = ranked.Where(r => r.Rank.CompareTo(best) == 0).Select(r => r.Seat).ToList();

        int share = pot.Total / winners.Count;
        int remainder = pot.Total % winners.Count;
        foreach (var winner in winners)
            winner.Chips += share;
        winners[0].Chips += remainder;

        io.WriteLine(winners.Count == 1
            ? $"{winners[0].Name} wins the pot of {pot.Total}!"
            : $"Split pot! {string.Join(", ", winners.Select(w => w.Name))} each win {share}.");
    }

    private static string Describe(HandRank rank)
    {
        bool isRoyal = rank.Category == HandCategory.StraightFlush && rank.Tiebreakers[0] == 13;
        return rank.Category switch
        {
            HandCategory.StraightFlush => isRoyal ? "a Royal Flush" : "a Straight Flush",
            HandCategory.FourOfAKind => "Four of a Kind",
            HandCategory.FullHouse => "a Full House",
            HandCategory.Flush => "a Flush",
            HandCategory.Straight => "a Straight",
            HandCategory.ThreeOfAKind => "Three of a Kind",
            HandCategory.TwoPair => "Two Pair",
            HandCategory.OnePair => "One Pair",
            _ => "High Card",
        };
    }
}
