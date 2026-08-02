using CardGames.Networking.Dtos;
using Microsoft.AspNetCore.SignalR.Client;

namespace CardGames.Networking.Client;

/// <summary>
/// Wraps a SignalR HubConnection for a remote player joining a hosted game.
/// </summary>
public sealed class GameClientConnection : IAsyncDisposable
{
    private readonly HubConnection _Connection;

    public event Action<IReadOnlyList<RosterEntry>>? RosterUpdated;

    /// <summary>Text from a Write/WriteLine call directed at this player, or broadcast to everyone. Render with Console.Write (not WriteLine) - the sender already embeds any trailing newline.</summary>
    public event Action<string>? MessageReceived;

    /// <summary>Fired when the host is now waiting on this player's next ReadLine() - call SubmitInputAsync in response.</summary>
    public event Action? PromptReceived;

    /// <summary>Fired once the connection to the host is permanently gone (reconnect attempts exhausted, or the host ended the session) - a signal to return to the local menu.</summary>
    public event Action<Exception?>? ConnectionClosed;

    /// <summary>Fired when the host explicitly aborted the session (e.g. another player disconnected mid-turn), with a human-readable reason.</summary>
    public event Action<string>? SessionAborted;

    public GameClientConnection(string host, int port)
    {
        _Connection = new HubConnectionBuilder()
            .WithUrl($"http://{host}:{port}/gamehub")
            .WithAutomaticReconnect()
            .Build();

        _Connection.On<IReadOnlyList<RosterEntry>>("RosterUpdated", roster => RosterUpdated?.Invoke(roster));
        _Connection.On<string>("Message", message => MessageReceived?.Invoke(message));
        _Connection.On("Prompted", () => PromptReceived?.Invoke());
        _Connection.On<string>("SessionAborted", reason => SessionAborted?.Invoke(reason));
        _Connection.Closed += exception =>
        {
            ConnectionClosed?.Invoke(exception);
            return Task.CompletedTask;
        };
    }

    public async Task<JoinResult> JoinAsync(string joinCode, string playerName, string pluginName, string pluginVersion)
    {
        await _Connection.StartAsync();
        return await _Connection.InvokeAsync<JoinResult>("JoinSession", joinCode, playerName, pluginName, pluginVersion);
    }

    public Task SubmitInputAsync(string text) => _Connection.InvokeAsync("SubmitInput", text);

    public async ValueTask DisposeAsync() => await _Connection.DisposeAsync();
}
