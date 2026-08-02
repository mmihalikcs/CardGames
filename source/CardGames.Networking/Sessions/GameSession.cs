using System.Collections.Concurrent;

namespace CardGames.Networking.Sessions;

/// <summary>
/// State for one hosted multiplayer game. v1 supports exactly one of these per process
/// (see GameSessionManager). The host plays locally (no SignalR connection of their own);
/// RequiredRemotePlayers is the number of additional human seats that must join before the
/// host can start the game.
/// </summary>
internal sealed class GameSession
{
    private int _NextJoinOrder;

    public string JoinCode { get; }

    public string HostPlayerName { get; }

    public int RequiredRemotePlayers { get; }

    public int AiOpponentCount { get; }

    public string HostPluginName { get; }

    public string HostPluginVersion { get; }

    public ConcurrentDictionary<string, PlayerSlot> Roster { get; } = new();

    /// <summary>Pending IGameIO.ReadLine() calls for remote seats, keyed by connection id, resolved by GameHub.SubmitInput.</summary>
    public ConcurrentDictionary<string, TaskCompletionSource<string?>> PendingReads { get; } = new();

    /// <summary>The background Task running IGameManager.StartGame(), set once the session starts.</summary>
    public Task? GameTask { get; set; }

    public bool IsFull => Roster.Count >= RequiredRemotePlayers;

    public GameSession(string joinCode, string hostPlayerName, int requiredRemotePlayers, int aiOpponentCount, string hostPluginName, string hostPluginVersion)
    {
        JoinCode = joinCode;
        HostPlayerName = hostPlayerName;
        RequiredRemotePlayers = requiredRemotePlayers;
        AiOpponentCount = aiOpponentCount;
        HostPluginName = hostPluginName;
        HostPluginVersion = hostPluginVersion;
    }

    public int NextJoinOrder() => Interlocked.Increment(ref _NextJoinOrder);
}
