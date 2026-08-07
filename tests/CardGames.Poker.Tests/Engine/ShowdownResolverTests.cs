using CardGames.Common.Tests;
using CardGames.Poker.Engine;
using CardGames.Poker.Tests.Fakes;
using Xunit;

namespace CardGames.Poker.Tests.Engine;

public sealed class ShowdownResolverTests
{
    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Resolve_SingleWinner_AwardsFullPot()
    {
        var winner = new Seat("Winner", true, 100);
        var loser = new Seat("Loser", false, 100);
        var pot = new Pot();
        pot.Add(50);
        var channel = new ScriptedGameChannel();

        ShowdownResolver.Resolve(
            new[] { winner, loser },
            seat => seat == winner
                ? new HandRank(HandCategory.OnePair, new[] { 5 })
                : new HandRank(HandCategory.HighCard, new[] { 13, 10, 8, 5, 2 }),
            pot,
            channel);

        Assert.Equal(150, winner.Chips);
        Assert.Equal(100, loser.Chips);
        Assert.Contains(channel.Published, e => e is PotAwarded { WinnerNames: ["Winner"], Total: 50 });
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Resolve_TiedHands_SplitsPotEvenly()
    {
        var a = new Seat("A", true, 100);
        var b = new Seat("B", false, 100);
        var pot = new Pot();
        pot.Add(40);
        var channel = new ScriptedGameChannel();

        var tiedRank = new HandRank(HandCategory.OnePair, new[] { 8, 5, 2 });
        ShowdownResolver.Resolve(new[] { a, b }, _ => tiedRank, pot, channel);

        Assert.Equal(120, a.Chips);
        Assert.Equal(120, b.Chips);
        Assert.Contains(channel.Published, e => e is PotAwarded { WinnerNames.Count: 2, SharePerWinner: 20 });
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Resolve_OddPotSplit_RemainderGoesToFirstWinner()
    {
        var a = new Seat("A", true, 0);
        var b = new Seat("B", false, 0);
        var pot = new Pot();
        pot.Add(41);
        var channel = new ScriptedGameChannel();

        var tiedRank = new HandRank(HandCategory.OnePair, new[] { 8, 5, 2 });
        ShowdownResolver.Resolve(new[] { a, b }, _ => tiedRank, pot, channel);

        Assert.Equal(21, a.Chips);
        Assert.Equal(20, b.Chips);
    }
}
