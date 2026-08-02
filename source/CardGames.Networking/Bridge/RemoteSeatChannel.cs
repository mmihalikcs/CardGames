using CardGames.Networking.Hubs;
using CardGames.Networking.Sessions;
using Microsoft.AspNetCore.SignalR;

namespace CardGames.Networking.Bridge;

/// <summary>
/// A remote human seat, backed by one SignalR connection. Write/WriteLine push a "Message" event
/// to that connection; ReadLine registers a pending-read TaskCompletionSource on the session
/// (keyed by connection id) and blocks the calling thread until GameHub.SubmitInput resolves it.
/// Safe to block on here because this always runs on the background Task driving StartGame(),
/// never on a SignalR request-handling thread.
/// </summary>
internal sealed class RemoteSeatChannel : ISeatChannel
{
    private readonly IHubContext<GameHub> _HubContext;
    private readonly GameSession _Session;
    private readonly string _ConnectionId;

    public RemoteSeatChannel(IHubContext<GameHub> hubContext, GameSession session, string connectionId)
    {
        _HubContext = hubContext;
        _Session = session;
        _ConnectionId = connectionId;
    }

    public void Write(string message) =>
        _HubContext.Clients.Client(_ConnectionId).SendAsync("Message", message).GetAwaiter().GetResult();

    public void WriteLine(string message) =>
        _HubContext.Clients.Client(_ConnectionId).SendAsync("Message", message + Environment.NewLine).GetAwaiter().GetResult();

    public string? ReadLine()
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _Session.PendingReads[_ConnectionId] = tcs;
        _HubContext.Clients.Client(_ConnectionId).SendAsync("Prompted").GetAwaiter().GetResult();
        return tcs.Task.GetAwaiter().GetResult();
    }
}
