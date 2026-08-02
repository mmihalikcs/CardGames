namespace CardGames.Domain.Interfaces;

/// <summary>
/// Optional extension of IGameIO for callers that need to address a specific participant.
/// Write/WriteLine/ReadLine calls made while a scope is active are routed to that participant
/// only; calls made with no active scope are broadcast to every participant. Implementations
/// that don't need per-participant routing (e.g. ConsoleGameIO) simply don't implement this
/// interface, so callers must treat its absence as "everything is broadcast/local".
/// </summary>
public interface ISeatContextGameIO : IGameIO
{
    IDisposable BeginParticipantScope(string participantId);
}
