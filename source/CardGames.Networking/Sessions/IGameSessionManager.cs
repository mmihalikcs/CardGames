using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;
using CardGames.Networking.Dtos;

namespace CardGames.Networking.Sessions;

public interface IGameSessionManager
{
    /// <summary>Starts hosting a new session (v1: replaces any existing session on this process). Returns the generated join code.</summary>
    string CreateSession(string hostPlayerName, int requiredRemotePlayers, int aiOpponentCount, string hostPluginName, string hostPluginVersion);

    /// <summary>
    /// Called by GameHub when a remote client attempts to join. Not intended for direct use from
    /// Presentation. pluginName/pluginVersion identify the plugin the joining client has loaded
    /// locally - the join is rejected if it doesn't match what the host is running, since a
    /// mismatched build can't safely share the game's protocol/variant set.
    /// </summary>
    JoinResult TryJoin(string joinCode, string playerName, string pluginName, string pluginVersion, string connectionId);

    /// <summary>Called by GameHub when a connection drops.</summary>
    void RemoveConnection(string connectionId);

    /// <summary>Called by GameHub.SubmitInput to resolve a remote seat's pending ReadLine().</summary>
    void CompletePendingRead(string connectionId, string? text);

    /// <summary>Called by GameHub.OnDisconnectedAsync so a seat blocked in ReadLine() throws instead of hanging forever.</summary>
    void FaultPendingRead(string connectionId, Exception exception);

    IReadOnlyList<RosterEntry> GetRoster();

    bool IsSessionFull { get; }

    string? CurrentJoinCode { get; }

    /// <summary>
    /// Builds the NetworkGameIO for the current (full) session and starts IGameManager.StartGame()
    /// on a background task, returning that task so the caller can await session completion
    /// instead of racing it for Console access. Throws if no session is open, the roster isn't
    /// full yet, or the server host hasn't started (no way to reach remote seats).
    /// </summary>
    Task StartSessionAsync(IPlugin plugin, GameVariant? variant);

    void EndSession();
}
