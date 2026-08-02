using CardGames.Domain.Interfaces;

namespace CardGames.Networking.Bridge;

/// <summary>
/// Routes IGameIO calls to a specific participant's ISeatChannel while a participant scope is
/// active (BeginParticipantScope), or broadcasts to every channel when unscoped. Setup-phase
/// prompts (which happen before any Seat/participant exists, so are always unscoped) are answered
/// from a pre-built queue derived from the lobby roster at session-start time, rather than a live
/// round-trip - see GameSessionManager.StartSession.
/// </summary>
public sealed class NetworkGameIO : IGameIO, ISeatContextGameIO
{
    private readonly Dictionary<string, ISeatChannel> _Channels;
    private readonly Queue<string> _SetupAnswers;
    private string? _CurrentParticipant;

    internal NetworkGameIO(IReadOnlyDictionary<string, ISeatChannel> channels, IEnumerable<string> setupAnswers)
    {
        _Channels = new Dictionary<string, ISeatChannel>(channels);
        _SetupAnswers = new Queue<string>(setupAnswers);
    }

    public IDisposable BeginParticipantScope(string participantId)
    {
        var previous = _CurrentParticipant;
        _CurrentParticipant = participantId;
        return new ScopeRestorer(this, previous);
    }

    public void Write(string message)
    {
        if (_CurrentParticipant == null)
        {
            foreach (var channel in _Channels.Values)
                channel.Write(message);
            return;
        }

        RequireScopedChannel().Write(message);
    }

    public void WriteLine(string message = "")
    {
        if (_CurrentParticipant == null)
        {
            foreach (var channel in _Channels.Values)
                channel.WriteLine(message);
            return;
        }

        RequireScopedChannel().WriteLine(message);
    }

    public string? ReadLine()
    {
        if (_CurrentParticipant == null)
            return _SetupAnswers.Count > 0 ? _SetupAnswers.Dequeue() : null;

        return RequireScopedChannel().ReadLine();
    }

    /// <summary>Throws if scoped to a participant with no registered channel - a typo'd id should fail loudly, not silently broadcast.</summary>
    private ISeatChannel RequireScopedChannel() =>
        _Channels.TryGetValue(_CurrentParticipant!, out var channel)
            ? channel
            : throw new InvalidOperationException($"No seat channel registered for participant '{_CurrentParticipant}'.");

    private sealed class ScopeRestorer : IDisposable
    {
        private readonly NetworkGameIO _Owner;
        private readonly string? _Previous;

        public ScopeRestorer(NetworkGameIO owner, string? previous)
        {
            _Owner = owner;
            _Previous = previous;
        }

        public void Dispose() => _Owner._CurrentParticipant = _Previous;
    }
}
