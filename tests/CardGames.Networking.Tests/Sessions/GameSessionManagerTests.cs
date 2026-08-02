using CardGames.Common.Tests;
using CardGames.Networking.Sessions;
using Xunit;

namespace CardGames.Networking.Tests.Sessions;

public sealed class GameSessionManagerTests
{
    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void CreateSession_RosterStartsWithJustTheHost()
    {
        var manager = new GameSessionManager();
        manager.CreateSession("Alice", requiredRemotePlayers: 1, aiOpponentCount: 2, "Poker", "1.0.0");

        var roster = manager.GetRoster();

        Assert.Single(roster);
        Assert.Equal("Alice", roster[0].PlayerName);
        Assert.True(roster[0].IsHost);
        Assert.False(manager.IsSessionFull);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void TryJoin_ValidCodeAndUniqueName_AddsToRosterAndFillsSession()
    {
        var manager = new GameSessionManager();
        var code = manager.CreateSession("Alice", requiredRemotePlayers: 1, aiOpponentCount: 2, "Poker", "1.0.0");

        var result = manager.TryJoin(code, "Bob", "Poker", "1.0.0", "conn-1");

        Assert.True(result.Success);
        Assert.Equal("Alice", result.HostPlayerName);
        Assert.Equal(2, result.Roster.Count);
        Assert.Contains(result.Roster, r => r.PlayerName == "Bob" && !r.IsHost);
        Assert.True(manager.IsSessionFull);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void TryJoin_PluginVersionMismatch_Fails()
    {
        var manager = new GameSessionManager();
        var code = manager.CreateSession("Alice", requiredRemotePlayers: 1, aiOpponentCount: 2, "Poker", "1.0.0");

        var result = manager.TryJoin(code, "Bob", "Poker", "1.1.0", "conn-1");

        Assert.False(result.Success);
        Assert.Contains("version mismatch", result.ErrorMessage);
        Assert.Contains("1.0.0", result.ErrorMessage);
        Assert.Contains("1.1.0", result.ErrorMessage);
        Assert.False(manager.IsSessionFull);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void TryJoin_WrongJoinCode_Fails()
    {
        var manager = new GameSessionManager();
        manager.CreateSession("Alice", requiredRemotePlayers: 1, aiOpponentCount: 2, "Poker", "1.0.0");

        var result = manager.TryJoin("WRONGCODE", "Bob", "Poker", "1.0.0", "conn-1");

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void TryJoin_DuplicateName_Fails()
    {
        var manager = new GameSessionManager();
        var code = manager.CreateSession("Alice", requiredRemotePlayers: 2, aiOpponentCount: 2, "Poker", "1.0.0");
        manager.TryJoin(code, "Bob", "Poker", "1.0.0", "conn-1");

        var result = manager.TryJoin(code, "bob", "Poker", "1.0.0", "conn-2"); // case-insensitive collision, and also collides with itself

        Assert.False(result.Success);
        Assert.Contains("already taken", result.ErrorMessage);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void TryJoin_NameCollidesWithHost_Fails()
    {
        var manager = new GameSessionManager();
        var code = manager.CreateSession("Alice", requiredRemotePlayers: 1, aiOpponentCount: 2, "Poker", "1.0.0");

        var result = manager.TryJoin(code, "alice", "Poker", "1.0.0", "conn-1");

        Assert.False(result.Success);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void TryJoin_SessionAlreadyFull_Fails()
    {
        var manager = new GameSessionManager();
        var code = manager.CreateSession("Alice", requiredRemotePlayers: 1, aiOpponentCount: 2, "Poker", "1.0.0");
        manager.TryJoin(code, "Bob", "Poker", "1.0.0", "conn-1");

        var result = manager.TryJoin(code, "Carol", "Poker", "1.0.0", "conn-2");

        Assert.False(result.Success);
        Assert.Contains("full", result.ErrorMessage);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void RemoveConnection_FreesSlotForAnotherPlayerToJoin()
    {
        var manager = new GameSessionManager();
        var code = manager.CreateSession("Alice", requiredRemotePlayers: 1, aiOpponentCount: 2, "Poker", "1.0.0");
        manager.TryJoin(code, "Bob", "Poker", "1.0.0", "conn-1");
        Assert.True(manager.IsSessionFull);

        manager.RemoveConnection("conn-1");
        Assert.False(manager.IsSessionFull);

        var result = manager.TryJoin(code, "Carol", "Poker", "1.0.0", "conn-2");
        Assert.True(result.Success);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void EndSession_ClearsCurrentJoinCodeAndRoster()
    {
        var manager = new GameSessionManager();
        manager.CreateSession("Alice", requiredRemotePlayers: 1, aiOpponentCount: 2, "Poker", "1.0.0");

        manager.EndSession();

        Assert.Null(manager.CurrentJoinCode);
        Assert.Empty(manager.GetRoster());
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void FaultPendingRead_ThrowsFromTheRegisteredReadOnceResolved()
    {
        var manager = new GameSessionManager();
        manager.CreateSession("Alice", requiredRemotePlayers: 1, aiOpponentCount: 2, "Poker", "1.0.0");
        var session = manager.CurrentSessionForTests!;
        var pendingRead = new TaskCompletionSource<string?>();
        session.PendingReads["conn-1"] = pendingRead;

        manager.FaultPendingRead("conn-1", new IOException("Player disconnected."));

        Assert.True(pendingRead.Task.IsFaulted);
        var thrown = Assert.IsType<IOException>(pendingRead.Task.Exception!.InnerException);
        Assert.Equal("Player disconnected.", thrown.Message);
        Assert.Empty(session.PendingReads);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void FaultPendingRead_UnknownConnection_DoesNothing()
    {
        var manager = new GameSessionManager();
        manager.CreateSession("Alice", requiredRemotePlayers: 1, aiOpponentCount: 2, "Poker", "1.0.0");

        var exception = Record.Exception(() => manager.FaultPendingRead("no-such-connection", new IOException("x")));

        Assert.Null(exception);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void FaultPendingRead_NoSessionOpen_DoesNotThrow()
    {
        var manager = new GameSessionManager();

        var exception = Record.Exception(() => manager.FaultPendingRead("conn-1", new IOException("x")));

        Assert.Null(exception);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public async Task CompletePendingRead_ResolvesTheRegisteredReadWithTheGivenText()
    {
        var manager = new GameSessionManager();
        manager.CreateSession("Alice", requiredRemotePlayers: 1, aiOpponentCount: 2, "Poker", "1.0.0");
        var session = manager.CurrentSessionForTests!;
        var pendingRead = new TaskCompletionSource<string?>();
        session.PendingReads["conn-1"] = pendingRead;

        manager.CompletePendingRead("conn-1", "fold");

        Assert.True(pendingRead.Task.IsCompletedSuccessfully);
        Assert.Equal("fold", await pendingRead.Task);
        Assert.Empty(session.PendingReads);
    }
}
