using CardGames.Domain.Interfaces;

namespace CardGames.Domain.Interaction;

/// <summary>
/// Generic adapter that implements the structured IGameChannel contract on top of any existing
/// IGameIO (console, network, test fake) by rendering events/prompts through Describe() and
/// parsing the raw text answer back into a typed PromptResponse. This is what lets ConsoleGameIO,
/// NetworkGameIO, and every ISeatChannel/SignalR/GameHub plumbing underneath NetworkGameIO stay
/// completely unchanged: they keep speaking raw text, and this wraps them once, at the point a
/// game manager is created, with zero per-plugin knowledge (GamePrompt's three kinds are the only
/// shapes it needs to know about - GameEvent leaf types are opaque, described via their own
/// Describe() override).
/// </summary>
public sealed class TextGameChannel : ISeatContextGameChannel
{
    private readonly IGameIO _Io;

    public TextGameChannel(IGameIO io)
    {
        _Io = io ?? throw new ArgumentNullException(nameof(io));
    }

    public void Publish(GameEvent gameEvent) => _Io.WriteLine(gameEvent.Describe());

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

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();
        public void Dispose() { }
    }
}
