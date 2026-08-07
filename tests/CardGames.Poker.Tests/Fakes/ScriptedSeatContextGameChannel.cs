using CardGames.Domain.Interaction;

namespace CardGames.Poker.Tests.Fakes;

/// <summary>
/// ISeatContextGameChannel test fake: records every published event and every awaited prompt
/// tagged with whichever participant scope was active at the time (null = broadcast/unscoped),
/// and answers Await from a per-participant scripted queue, falling back to a shared default
/// response when a queue is empty or unspecified.
/// </summary>
internal sealed class ScriptedSeatContextGameChannel : ISeatContextGameChannel
{
    private readonly Dictionary<string, Queue<string?>> _InputByParticipant;
    private readonly string? _DefaultResponse;
    private string? _CurrentParticipant;

    public List<(string? ParticipantId, GameEvent Event)> Published { get; } = new();
    public List<(string? ParticipantId, GamePrompt Prompt)> Awaited { get; } = new();

    public ScriptedSeatContextGameChannel(IReadOnlyDictionary<string, IEnumerable<string?>>? inputByParticipant = null, string? defaultResponse = null)
    {
        _InputByParticipant = (inputByParticipant ?? new Dictionary<string, IEnumerable<string?>>())
            .ToDictionary(kv => kv.Key, kv => new Queue<string?>(kv.Value));
        _DefaultResponse = defaultResponse;
    }

    public IDisposable BeginParticipantScope(string participantId)
    {
        var previous = _CurrentParticipant;
        _CurrentParticipant = participantId;
        return new ScopeRestorer(this, previous);
    }

    public void Publish(GameEvent gameEvent) => Published.Add((_CurrentParticipant, gameEvent));

    public PromptResponse Await(GamePrompt prompt)
    {
        Awaited.Add((_CurrentParticipant, prompt));

        string? raw;
        if (_CurrentParticipant != null && _InputByParticipant.TryGetValue(_CurrentParticipant, out var queue) && queue.Count > 0)
            raw = queue.Dequeue();
        else if (_DefaultResponse != null)
            raw = _DefaultResponse;
        else
            throw new InvalidOperationException($"ScriptedSeatContextGameChannel.Await called for '{_CurrentParticipant ?? "<none>"}' with no scripted input remaining.");

        return prompt switch
        {
            ConfirmPrompt => new Ack(),
            ChoicePrompt => new ChoiceResponse(raw ?? string.Empty),
            TextPrompt => new TextResponse(raw ?? string.Empty),
            _ => throw new NotSupportedException(prompt.GetType().Name)
        };
    }

    public IReadOnlyList<GameEvent> EventsFor(string participantId) =>
        Published.Where(o => o.ParticipantId == participantId).Select(o => o.Event).ToList();

    public IReadOnlyList<GameEvent> BroadcastEvents =>
        Published.Where(o => o.ParticipantId == null).Select(o => o.Event).ToList();

    public IReadOnlyList<GamePrompt> PromptsFor(string participantId) =>
        Awaited.Where(o => o.ParticipantId == participantId).Select(o => o.Prompt).ToList();

    private sealed class ScopeRestorer : IDisposable
    {
        private readonly ScriptedSeatContextGameChannel _Owner;
        private readonly string? _Previous;

        public ScopeRestorer(ScriptedSeatContextGameChannel owner, string? previous)
        {
            _Owner = owner;
            _Previous = previous;
        }

        public void Dispose() => _Owner._CurrentParticipant = _Previous;
    }
}
