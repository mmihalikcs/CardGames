namespace CardGames.Domain.Interaction;

/// <summary>Structured replacement for IGameIO's Write/WriteLine: publishes facts, not formatted prose.</summary>
public interface IGameEventSink
{
    void Publish(GameEvent gameEvent);
}

/// <summary>
/// Structured replacement for IGameIO's ReadLine: requests a typed answer from a specific seat.
/// Synchronous by design, same as today's IGameIO.ReadLine - a seat-routing layer decides which
/// underlying transport this blocks on (console input, a SignalR round-trip, a Godot UI event).
/// </summary>
public interface IGamePromptChannel
{
    PromptResponse Await(GamePrompt prompt);
}

public interface IGameChannel : IGameEventSink, IGamePromptChannel
{
}

/// <summary>
/// Optional extension of IGameChannel for callers that need to address a specific participant -
/// the structured-contract counterpart of ISeatContextGameIO. Write/Publish/Await calls made while
/// a scope is active are routed to that participant only; calls made with no active scope broadcast.
/// </summary>
public interface ISeatContextGameChannel : IGameChannel
{
    IDisposable BeginParticipantScope(string participantId);
}
