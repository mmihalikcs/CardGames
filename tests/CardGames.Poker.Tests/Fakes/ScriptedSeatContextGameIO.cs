using CardGames.Domain.Interfaces;

namespace CardGames.Poker.Tests.Fakes;

/// <summary>
/// ISeatContextGameIO test fake: records every Write/WriteLine tagged with whichever participant
/// scope was active at the time (null = broadcast/unscoped), and answers ReadLine from a
/// per-participant scripted queue, falling back to a shared default response when a queue is
/// empty or unspecified.
/// </summary>
internal sealed class ScriptedSeatContextGameIO : IGameIO, ISeatContextGameIO
{
    private readonly Dictionary<string, Queue<string?>> _InputByParticipant;
    private readonly string? _DefaultResponse;
    private string? _CurrentParticipant;

    public List<(string? ParticipantId, string Message)> Output { get; } = new();

    public ScriptedSeatContextGameIO(IReadOnlyDictionary<string, IEnumerable<string?>>? inputByParticipant = null, string? defaultResponse = null)
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

    public void Write(string message) => Output.Add((_CurrentParticipant, message));

    public void WriteLine(string message = "") => Output.Add((_CurrentParticipant, message));

    public string? ReadLine()
    {
        if (_CurrentParticipant != null && _InputByParticipant.TryGetValue(_CurrentParticipant, out var queue) && queue.Count > 0)
            return queue.Dequeue();
        if (_DefaultResponse != null)
            return _DefaultResponse;
        throw new InvalidOperationException($"ScriptedSeatContextGameIO.ReadLine called for '{_CurrentParticipant ?? "<none>"}' with no scripted input remaining.");
    }

    public IReadOnlyList<string> OutputFor(string participantId) =>
        Output.Where(o => o.ParticipantId == participantId).Select(o => o.Message).ToList();

    public IReadOnlyList<string> BroadcastOutput =>
        Output.Where(o => o.ParticipantId == null).Select(o => o.Message).ToList();

    private sealed class ScopeRestorer : IDisposable
    {
        private readonly ScriptedSeatContextGameIO _Owner;
        private readonly string? _Previous;

        public ScopeRestorer(ScriptedSeatContextGameIO owner, string? previous)
        {
            _Owner = owner;
            _Previous = previous;
        }

        public void Dispose() => _Owner._CurrentParticipant = _Previous;
    }
}
