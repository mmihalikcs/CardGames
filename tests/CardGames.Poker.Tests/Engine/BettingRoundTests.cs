using CardGames.Common.Tests;
using CardGames.Poker.Engine;
using CardGames.Poker.Tests.Fakes;
using Xunit;

namespace CardGames.Poker.Tests.Engine;

public sealed class BettingRoundTests
{
    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void RunStreet_AllCheck_NoChipsMove()
    {
        var seats = new[] { new Seat("A", false, 500), new Seat("B", false, 500) };
        var pot = new Pot();
        var io = new ScriptedGameIO();
        var ai = new AiDecisionMaker(new Random());

        // strength 0.0 is always below RaiseThreshold, so with toCall == 0 the raise branch
        // short-circuits before touching the Random - deterministic regardless of RNG sequence.
        var round = new BettingRound(seats, pot, io, ai, _ => 0.0);

        var result = round.RunStreet();

        Assert.True(result);
        Assert.Equal(500, seats[0].Chips);
        Assert.Equal(500, seats[1].Chips);
        Assert.Equal(0, pot.Total);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void RunStreet_RaiseThenAllCall_PotMatchesContributions()
    {
        var alice = new Seat("Alice", true, 500);
        var bob = new Seat("Bob", true, 500);
        var pot = new Pot();
        var io = new ScriptedGameIO(new[] { "raise", "call" });
        var ai = new AiDecisionMaker(new Random());

        var round = new BettingRound(new[] { alice, bob }, pot, io, ai, _ => 0.0);
        var result = round.RunStreet();

        Assert.True(result);
        Assert.Equal(20, pot.Total);
        Assert.Equal(490, alice.Chips);
        Assert.Equal(490, bob.Chips);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void RunStreet_FoldsToOneRemaining_ReturnsFalse()
    {
        var alice = new Seat("Alice", true, 500);
        var bob = new Seat("Bob", true, 500);
        var pot = new Pot();
        var io = new ScriptedGameIO(new[] { "fold" });
        var ai = new AiDecisionMaker(new Random());

        var round = new BettingRound(new[] { alice, bob }, pot, io, ai, _ => 0.0);
        var result = round.RunStreet();

        Assert.False(result);
        Assert.True(alice.HasFolded);
        Assert.False(bob.HasFolded);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void RunStreet_RaiseCapEnforced_RejectsExtraRaiseAttempt()
    {
        var alice = new Seat("Alice", true, 500);
        var bob = new Seat("Bob", true, 500);
        var pot = new Pot();
        // Bob's first "raise" attempt is rejected (cap already hit by Alice) and re-prompted.
        var io = new ScriptedGameIO(new[] { "raise", "raise", "call" });
        var ai = new AiDecisionMaker(new Random());

        var round = new BettingRound(new[] { alice, bob }, pot, io, ai, _ => 0.0, maxRaises: 1);
        var result = round.RunStreet();

        Assert.True(result);
        Assert.Equal(20, pot.Total);
        Assert.Equal(490, bob.Chips);
        Assert.Contains("Invalid action", io.AllOutput);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void RunStreet_CannotAffordRaise_GoesAllInForLess()
    {
        var alice = new Seat("Alice", true, 5); // less than the default 10-chip bet increment
        var bob = new Seat("Bob", true, 500);
        var pot = new Pot();
        var io = new ScriptedGameIO(new[] { "raise", "call" });
        var ai = new AiDecisionMaker(new Random());

        var round = new BettingRound(new[] { alice, bob }, pot, io, ai, _ => 0.0);
        var result = round.RunStreet();

        Assert.True(result);
        Assert.True(alice.IsAllIn);
        Assert.Equal(0, alice.Chips);
        Assert.Equal(10, pot.Total);
        Assert.Equal(495, bob.Chips);
    }
}
