using CardGames.Common.Tests;
using CardGames.Networking.Bridge;
using CardGames.Networking.Tests.Fakes;
using Xunit;

namespace CardGames.Networking.Tests.Bridge;

public sealed class NetworkGameIOTests
{
    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Write_Unscoped_BroadcastsToEveryChannel()
    {
        var alice = new RecordingSeatChannel();
        var bob = new RecordingSeatChannel();
        var io = new NetworkGameIO(new Dictionary<string, ISeatChannel> { ["Alice"] = alice, ["Bob"] = bob }, Array.Empty<string>());

        io.WriteLine("Community cards: ...");

        Assert.Contains("Community cards: ...", alice.Output);
        Assert.Contains("Community cards: ...", bob.Output);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Write_WithinParticipantScope_RoutesOnlyToThatChannel()
    {
        var alice = new RecordingSeatChannel();
        var bob = new RecordingSeatChannel();
        var io = new NetworkGameIO(new Dictionary<string, ISeatChannel> { ["Alice"] = alice, ["Bob"] = bob }, Array.Empty<string>());

        using (io.BeginParticipantScope("Alice"))
            io.WriteLine("Alice's hole cards: ...");

        Assert.Contains("Alice's hole cards: ...", alice.Output);
        Assert.DoesNotContain("Alice's hole cards: ...", bob.Output);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void ScopeDispose_RestoresThePreviousScope()
    {
        var alice = new RecordingSeatChannel();
        var bob = new RecordingSeatChannel();
        var io = new NetworkGameIO(new Dictionary<string, ISeatChannel> { ["Alice"] = alice, ["Bob"] = bob }, Array.Empty<string>());

        using (io.BeginParticipantScope("Alice"))
        {
            using (io.BeginParticipantScope("Bob"))
                io.WriteLine("to Bob");

            io.WriteLine("back to Alice");
        }
        io.WriteLine("unscoped again");

        Assert.Equal(new[] { "back to Alice", "unscoped again" }, alice.Output);
        Assert.Equal(new[] { "to Bob", "unscoped again" }, bob.Output);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void ReadLine_WithinParticipantScope_DelegatesToThatChannel()
    {
        var alice = new RecordingSeatChannel("call");
        var bob = new RecordingSeatChannel("fold");
        var io = new NetworkGameIO(new Dictionary<string, ISeatChannel> { ["Alice"] = alice, ["Bob"] = bob }, Array.Empty<string>());

        string? aliceAnswer, bobAnswer;
        using (io.BeginParticipantScope("Alice"))
            aliceAnswer = io.ReadLine();
        using (io.BeginParticipantScope("Bob"))
            bobAnswer = io.ReadLine();

        Assert.Equal("call", aliceAnswer);
        Assert.Equal("fold", bobAnswer);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void ReadLine_Unscoped_DrainsSetupAnswersInOrderThenReturnsNull()
    {
        var io = new NetworkGameIO(new Dictionary<string, ISeatChannel>(), new[] { "2", "Bob", "2" });

        Assert.Equal("2", io.ReadLine());
        Assert.Equal("Bob", io.ReadLine());
        Assert.Equal("2", io.ReadLine());
        Assert.Null(io.ReadLine());
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void ReadLine_ScopedToUnregisteredParticipant_ThrowsRatherThanSilentlyFallingBack()
    {
        var io = new NetworkGameIO(new Dictionary<string, ISeatChannel>(), Array.Empty<string>());

        using (io.BeginParticipantScope("Nobody"))
            Assert.Throws<InvalidOperationException>(() => io.ReadLine());
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Write_ScopedToUnregisteredParticipant_ThrowsRatherThanSilentlyBroadcasting()
    {
        var alice = new RecordingSeatChannel();
        var io = new NetworkGameIO(new Dictionary<string, ISeatChannel> { ["Alice"] = alice }, Array.Empty<string>());

        using (io.BeginParticipantScope("Nobody"))
            Assert.Throws<InvalidOperationException>(() => io.WriteLine("oops"));

        Assert.Empty(alice.Output);
    }
}
