using CardGames.Domain.Interaction;
using CardGames.Domain.Models;

namespace CardGames.Poker.Engine;

/// <summary>
/// Publishes the card-reveal events; ASCII/graphical card rendering itself is generic now, driven
/// by each event's GameEvent.CardGroups (see TextGameChannel for the console's ASCII rendering).
/// AI hole cards must only ever be shown at showdown.
/// </summary>
internal static class TableRenderer
{
    public static void ShowHoleCards(IGameChannel io, Seat seat) =>
        io.Publish(new HoleCardsRevealed(seat.Name, seat.HoleCards));

    public static void ShowCommunityCards(IGameChannel io, IReadOnlyList<Card> community)
    {
        if (community.Count == 0)
            return;

        io.Publish(new CommunityCardsRevealed(community));
    }

    public static void ShowStacks(IGameChannel io, IReadOnlyList<Seat> seats)
    {
        var lines = seats.Select(seat =>
            new StackLine(seat.Name, seat.Chips, seat.HasFolded ? "folded" : seat.IsAllIn ? "all-in" : "active")).ToList();
        io.Publish(new StacksStatus(lines));
    }
}
