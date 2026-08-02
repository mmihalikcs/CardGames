using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;
using CardGames.Networking.Bridge;
using CardGames.Networking.Dtos;
using CardGames.Networking.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CardGames.Networking.Sessions;

/// <summary>
/// v1 supports exactly one hosted session per process. All operations are guarded by a single
/// lock since GameHub methods run concurrently on SignalR's threadpool dispatch threads.
/// </summary>
public sealed class GameSessionManager : IGameSessionManager
{
    private readonly object _Lock = new();
    private GameSession? _Session;
    private IHubContext<GameHub>? _HubContext;

    public string CreateSession(string hostPlayerName, int requiredRemotePlayers, int aiOpponentCount, string hostPluginName, string hostPluginVersion)
    {
        lock (_Lock)
        {
            var joinCode = GenerateJoinCode();
            _Session = new GameSession(joinCode, hostPlayerName, requiredRemotePlayers, aiOpponentCount, hostPluginName, hostPluginVersion);
            return joinCode;
        }
    }

    public JoinResult TryJoin(string joinCode, string playerName, string pluginName, string pluginVersion, string connectionId)
    {
        lock (_Lock)
        {
            var session = _Session;
            if (session == null || !string.Equals(session.JoinCode, joinCode, StringComparison.OrdinalIgnoreCase))
                return new JoinResult(false, "No open session with that join code.", null, Array.Empty<RosterEntry>());

            if (session.IsFull)
                return new JoinResult(false, "This session is already full.", null, Array.Empty<RosterEntry>());

            var trimmedName = playerName.Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
                return new JoinResult(false, "A player name is required.", null, Array.Empty<RosterEntry>());

            // "You" is the fixed local seat name the poker engine uses for the host - reserve it
            // so a remote player can never collide with it (see NetworkGameIO channel keys).
            if (string.Equals(trimmedName, "You", StringComparison.OrdinalIgnoreCase))
                return new JoinResult(false, "'You' is reserved for the host's own seat.", null, Array.Empty<RosterEntry>());

            bool nameTaken = string.Equals(session.HostPlayerName, trimmedName, StringComparison.OrdinalIgnoreCase)
                || session.Roster.Values.Any(p => string.Equals(p.PlayerName, trimmedName, StringComparison.OrdinalIgnoreCase));
            if (nameTaken)
                return new JoinResult(false, $"'{trimmedName}' is already taken in this session.", null, Array.Empty<RosterEntry>());

            // Ordinal, not OrdinalIgnoreCase like the name checks above - these are build
            // identifiers, not human display text, so a mismatch here means a genuinely different
            // plugin build that can't be trusted to speak the same protocol/variant set.
            if (!string.Equals(session.HostPluginName, pluginName, StringComparison.Ordinal)
                || !string.Equals(session.HostPluginVersion, pluginVersion, StringComparison.Ordinal))
            {
                return new JoinResult(
                    false,
                    $"Game version mismatch: host is running {session.HostPluginName} v{session.HostPluginVersion}, you have {pluginName} v{pluginVersion}.",
                    null,
                    Array.Empty<RosterEntry>());
            }

            session.Roster[connectionId] = new PlayerSlot(connectionId, trimmedName, session.NextJoinOrder());
            return new JoinResult(true, null, session.HostPlayerName, BuildRoster(session));
        }
    }

    public void RemoveConnection(string connectionId)
    {
        lock (_Lock)
        {
            _Session?.Roster.TryRemove(connectionId, out _);
        }
    }

    public void CompletePendingRead(string connectionId, string? text)
    {
        GameSession? session;
        lock (_Lock) { session = _Session; }

        if (session != null && session.PendingReads.TryRemove(connectionId, out var tcs))
            tcs.TrySetResult(text);
    }

    public void FaultPendingRead(string connectionId, Exception exception)
    {
        GameSession? session;
        lock (_Lock) { session = _Session; }

        if (session != null && session.PendingReads.TryRemove(connectionId, out var tcs))
            tcs.TrySetException(exception);
    }

    public IReadOnlyList<RosterEntry> GetRoster()
    {
        lock (_Lock)
        {
            return _Session == null ? Array.Empty<RosterEntry>() : BuildRoster(_Session);
        }
    }

    public bool IsSessionFull
    {
        get { lock (_Lock) { return _Session?.IsFull ?? false; } }
    }

    public string? CurrentJoinCode
    {
        get { lock (_Lock) { return _Session?.JoinCode; } }
    }

    public Task StartSessionAsync(IPlugin plugin, GameVariant? variant)
    {
        GameSession session;
        IHubContext<GameHub> hubContext;
        lock (_Lock)
        {
            session = _Session ?? throw new InvalidOperationException("No session is currently open.");
            if (!session.IsFull)
                throw new InvalidOperationException("The session is not yet full.");
            hubContext = _HubContext ?? throw new InvalidOperationException("The game server host has not started yet.");
        }

        var orderedRemotePlayers = session.Roster.Values.OrderBy(p => p.JoinOrder).ToList();

        var channels = new Dictionary<string, ISeatChannel> { ["You"] = new LocalConsoleSeatChannel() };
        foreach (var slot in orderedRemotePlayers)
            channels[slot.PlayerName] = new RemoteSeatChannel(hubContext, session, slot.ConnectionId);

        // Mirrors SeatSetup.BuildSeats' prompt order exactly: human count, then one name per
        // additional human (join order), then AI opponent count.
        var setupAnswers = new List<string> { (1 + session.RequiredRemotePlayers).ToString() };
        setupAnswers.AddRange(orderedRemotePlayers.Select(p => p.PlayerName));
        setupAnswers.Add(session.AiOpponentCount.ToString());

        var networkIo = new NetworkGameIO(channels, setupAnswers);
        var gameManager = variant == null ? plugin.CreateGameManager(networkIo) : plugin.CreateGameManager(networkIo, variant);

        var gameTask = Task.Run(() => gameManager.StartGame());
        session.GameTask = gameTask;
        return AwaitAndNotifyOnFailureAsync(gameTask, session, hubContext);
    }

    /// <summary>
    /// If the game task faults (e.g. a disconnect faulted a remote seat's ReadLine()), tells
    /// whichever other remote players are still connected that the session aborted before
    /// rethrowing, so the caller (Presentation) still sees the original exception.
    /// </summary>
    private static async Task AwaitAndNotifyOnFailureAsync(Task gameTask, GameSession session, IHubContext<GameHub> hubContext)
    {
        try
        {
            await gameTask;
        }
        catch (Exception ex)
        {
            var stillConnected = session.Roster.Values.Select(p => p.ConnectionId).ToList();
            if (stillConnected.Count > 0)
                await hubContext.Clients.Clients(stillConnected).SendAsync("SessionAborted", ex.Message);
            throw;
        }
    }

    /// <summary>Called by GameServerHost once its own WebApplication (and thus its SignalR hub context) is up.</summary>
    internal void AttachHubContext(IHubContext<GameHub> hubContext)
    {
        lock (_Lock) { _HubContext = hubContext; }
    }

    /// <summary>Test seam - the currently open session, if any.</summary>
    internal GameSession? CurrentSessionForTests => _Session;

    public void EndSession()
    {
        lock (_Lock)
        {
            _Session = null;
            _HubContext = null;
        }
    }

    private static IReadOnlyList<RosterEntry> BuildRoster(GameSession session)
    {
        var roster = new List<RosterEntry> { new(session.HostPlayerName, IsHost: true) };
        roster.AddRange(session.Roster.Values.OrderBy(p => p.JoinOrder).Select(p => new RosterEntry(p.PlayerName, IsHost: false)));
        return roster;
    }

    private static string GenerateJoinCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/1/I, avoids ambiguity when read aloud/typed
        var random = Random.Shared;
        return new string(Enumerable.Range(0, 6).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());
    }
}
