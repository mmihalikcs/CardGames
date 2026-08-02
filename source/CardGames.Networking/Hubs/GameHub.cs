using CardGames.Networking.Dtos;
using CardGames.Networking.Sessions;
using Microsoft.AspNetCore.SignalR;

namespace CardGames.Networking.Hubs;

/// <summary>
/// Deliberately thin - delegates all session/roster logic to IGameSessionManager so it stays
/// testable without a real Hub context.
/// </summary>
public sealed class GameHub : Hub
{
    private readonly IGameSessionManager _SessionManager;

    public GameHub(IGameSessionManager sessionManager)
    {
        _SessionManager = sessionManager;
    }

    public async Task<JoinResult> JoinSession(string joinCode, string playerName, string pluginName, string pluginVersion)
    {
        var result = _SessionManager.TryJoin(joinCode, playerName, pluginName, pluginVersion, Context.ConnectionId);
        if (!result.Success)
            return result;

        await Groups.AddToGroupAsync(Context.ConnectionId, joinCode);
        await Clients.Group(joinCode).SendAsync("RosterUpdated", _SessionManager.GetRoster());
        return result;
    }

    public Task SubmitInput(string text)
    {
        _SessionManager.CompletePendingRead(Context.ConnectionId, text);
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        // If this connection was mid-turn (blocked in RemoteSeatChannel.ReadLine()), fault that
        // wait instead of leaving the game thread hung forever - v1 has no reconnect/resume, a
        // disconnect simply aborts the session (see GameSessionManager.AwaitAndNotifyOnFailureAsync).
        _SessionManager.FaultPendingRead(Context.ConnectionId, new IOException("Player disconnected."));
        _SessionManager.RemoveConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
