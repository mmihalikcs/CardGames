using CardGames.Domain.Models;

namespace CardGames.Poker;

internal static class PokerVariants
{
    internal const string TexasHoldemKey = "texas-holdem";
    internal const string OmahaKey = "omaha";
    internal const string FiveCardDrawKey = "five-card-draw";

    internal static readonly GameVariant TexasHoldem = new(
        TexasHoldemKey, "Texas Hold'em", "2 hole cards, 5 shared community cards, best 5 of 7.");

    internal static readonly GameVariant Omaha = new(
        OmahaKey, "Omaha", "4 hole cards; must use exactly 2 hole + 3 community cards.");

    internal static readonly GameVariant FiveCardDraw = new(
        FiveCardDrawKey, "Five-Card Draw", "5 private cards, one betting round, a draw, then a final betting round.");

    internal static IReadOnlyList<GameVariant> All { get; } = new[] { TexasHoldem, Omaha, FiveCardDraw };
}
