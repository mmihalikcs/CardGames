using CardGames.Networking.Client;
using CardGames.Networking.Dtos;
using CardGames.Networking.Hosting;
using CardGames.Networking.Sessions;
using Xunit;

namespace CardGames.Networking.Tests;

/// <summary>
/// Exercises a real Kestrel-hosted SignalR hub over a real loopback socket (ephemeral port),
/// proving GameServerHost/GameClientConnection actually work end to end - not just the
/// transport-free GameSessionManager logic covered elsewhere. Deliberately untagged (not
/// [Trait(category, post-build)]) so it stays out of the fast CI gate; real socket binding is
/// slower and more environment-dependent than the fake-based unit tests.
/// </summary>
public sealed class GameServerConnectivityTests
{
    [Fact]
    public async Task HostAndClient_RealKestrelAndSignalR_ClientCanJoinAndSeeRosterUpdate()
    {
        var sessionManager = new GameSessionManager();
        var joinCode = sessionManager.CreateSession("Alice", requiredRemotePlayers: 1, aiOpponentCount: 2, "Poker", "1.0.0");

        await using var host = new GameServerHost();
        await host.StartAsync(sessionManager, port: 0);

        await using var client = new GameClientConnection("127.0.0.1", host.Port!.Value);

        IReadOnlyList<RosterEntry>? observedRoster = null;
        client.RosterUpdated += roster => observedRoster = roster;

        var result = await client.JoinAsync(joinCode, "Bob", "Poker", "1.0.0");

        Assert.True(result.Success);
        Assert.Equal("Alice", result.HostPlayerName);
        Assert.Contains(result.Roster, r => r.PlayerName == "Bob");

        for (int i = 0; i < 50 && observedRoster == null; i++)
            await Task.Delay(20);

        Assert.NotNull(observedRoster);
        Assert.Contains(observedRoster!, r => r.PlayerName == "Bob");
    }
}
