using System.Text;
using CardGames.Domain.Extensions;
using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;

namespace CardGames.Domain.Interaction;

/// <summary>
/// Generic adapter that implements the structured IGameChannel contract on top of any existing
/// IGameIO (console, network, test fake) by rendering events/prompts through Describe() and
/// parsing the raw text answer back into a typed PromptResponse. This is what lets ConsoleGameIO,
/// NetworkGameIO, and every ISeatChannel/SignalR/GameHub plumbing underneath NetworkGameIO stay
/// completely unchanged: they keep speaking raw text, and this wraps them once, at the point a
/// game manager is created, with zero per-plugin knowledge (GamePrompt's three kinds and
/// GameEvent.CardGroups are the only shapes it needs to know about - GameEvent leaf types
/// themselves are opaque, described via their own Describe() override).
/// </summary>
public sealed class TextGameChannel : ISeatContextGameChannel
{
    private readonly IGameIO _Io;

    public TextGameChannel(IGameIO io)
    {
        _Io = io ?? throw new ArgumentNullException(nameof(io));
    }

    public void Publish(GameEvent gameEvent)
    {
        _Io.WriteLine(gameEvent.Describe());
        if (gameEvent.CardGroups.Count > 0)
            _Io.Write(RenderCardGroups(gameEvent.CardGroups));
    }

    public PromptResponse Await(GamePrompt prompt)
    {
        _Io.Write(prompt.Describe());
        var raw = _Io.ReadLine() ?? string.Empty;

        return prompt switch
        {
            ConfirmPrompt => new Ack(),
            ChoicePrompt => new ChoiceResponse(raw.Trim()),
            TextPrompt => new TextResponse(raw),
            _ => throw new NotSupportedException($"Unknown GamePrompt kind '{prompt.GetType().Name}'.")
        };
    }

    public IDisposable BeginParticipantScope(string participantId) =>
        (_Io as ISeatContextGameIO)?.BeginParticipantScope(participantId) ?? NoopScope.Instance;

    // Generalizes what used to be duplicated per plugin (WAR's RenderVersus, Poker's
    // TableRenderer.RenderCardRow, GoFish's RenderHandRow): one group per labeled row, cards drawn
    // side by side within a group via Card.DisplayCard()'s ASCII box art.
    private static string RenderCardGroups(IReadOnlyList<CardGroup> groups)
    {
        var sb = new StringBuilder();
        foreach (var group in groups)
        {
            if (!string.IsNullOrEmpty(group.Label))
                sb.AppendLine($"{group.Label}:");
            sb.Append(RenderCardRow(group.Cards));
        }
        return sb.ToString();
    }

    private static string RenderCardRow(IReadOnlyList<Card> cards)
    {
        if (cards.Count == 0)
            return string.Empty;

        var cardLines = cards.Select(c => c.DisplayCard().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)).ToList();
        var lineCount = cardLines[0].Length;

        var sb = new StringBuilder();
        for (var line = 0; line < lineCount; line++)
            sb.AppendLine(string.Join("  ", cardLines.Select(l => l[line])));
        return sb.ToString();
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();
        public void Dispose() { }
    }
}
