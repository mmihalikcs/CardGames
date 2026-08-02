using CardGames.Domain.Extensions;
using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;
using System.Text;

namespace CardGames.Poker.Engine;

/// <summary>
/// Console rendering helpers built on Card.DisplayCard(), generalizing WAR's RenderVersus
/// side-by-side technique to N cards. AI hole cards must only ever be shown at showdown.
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

    public static void ShowHoleCards(IGameIO io, Seat seat)
    {
        io.WriteLine($"{seat.Name}'s hole cards:");
        io.WriteLine(RenderCardRow(seat.HoleCards));
    }

    public static void ShowCommunityCards(IGameIO io, IReadOnlyList<Card> community)
    {
        if (community.Count == 0)
            return;

        io.WriteLine("Community cards:");
        io.WriteLine(RenderCardRow(community));
    }

    public static void ShowStacks(IGameIO io, IReadOnlyList<Seat> seats)
    {
        foreach (var seat in seats)
        {
            var status = seat.HasFolded ? "folded" : seat.IsAllIn ? "all-in" : "active";
            io.WriteLine($"{seat.Name}: {seat.Chips} chips ({status})");
        }
    }
}
