using CardGames.Domain.Extensions;
using CardGames.Domain.Interaction;
using CardGames.Domain.Models;
using System.Text;

namespace CardGames.Poker.Engine;

/// <summary>
/// Rendering helpers built on Card.DisplayCard(), generalizing WAR's RenderVersus side-by-side
/// technique to N cards. AI hole cards must only ever be shown at showdown.
/// </summary>
internal static class TableRenderer
{
    public static string RenderCardRow(IReadOnlyList<Card> cards)
    {
        if (cards.Count == 0)
            return string.Empty;

        var cardLines = cards.Select(c => c.DisplayCard().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)).ToList();
        int lineCount = cardLines[0].Length;

        var sb = new StringBuilder();
        for (int line = 0; line < lineCount; line++)
        {
            sb.AppendLine(string.Join("   ", cardLines.Select(lines => lines[line])));
        }
        return sb.ToString();
    }

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
