using CardGames.Networking.Hubs;
using CardGames.Networking.Sessions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CardGames.Networking.Hosting;

/// <summary>
/// Wraps a dedicated Kestrel+SignalR WebApplication, started only when a player chooses to host
/// a multiplayer game - never at app startup - and stopped when the session ends. This is
/// independent of Presentation's own generic IHost.
/// </summary>
public sealed class GameServerHost : IAsyncDisposable
{
    private WebApplication? _App;

    public int? Port { get; private set; }

    public async Task StartAsync(IGameSessionManager sessionManager, int port = 0)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        builder.Services.AddSingleton(sessionManager);
        builder.Services.AddSignalR();

        _App = builder.Build();
        _App.MapHub<GameHub>("/gamehub");

        await _App.StartAsync();
        Port = new Uri(_App.Urls.First()).Port;

        // The session manager needs a way to reach remote seats once it starts a session - that's
        // only available from this WebApplication's own DI container, not Presentation's.
        if (sessionManager is GameSessionManager concrete)
            concrete.AttachHubContext(_App.Services.GetRequiredService<IHubContext<GameHub>>());
    }

    public async Task StopAsync()
    {
        if (_App == null)
            return;

        await _App.StopAsync();
        await _App.DisposeAsync();
        _App = null;
        Port = null;
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
