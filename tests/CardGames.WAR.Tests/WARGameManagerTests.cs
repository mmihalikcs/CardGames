using CardGames.Common.Tests;
using CardGames.Domain.Enums;
using CardGames.Domain.Models;
using CardGames.WAR.Tests.Fakes;
using Xunit;

namespace CardGames.WAR.Tests;

public sealed class WARGameManagerTests
{
    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_StartGame_PlayerWinsWithoutTies()
    {
        var playerHand = new Queue<Card>(new[]
        {
            new Card(Suit.Spades, Rank.Ace),
            new Card(Suit.Spades, Rank.King),
        });
        var computerHand = new Queue<Card>(new[]
        {
            new Card(Suit.Hearts, Rank.Two),
            new Card(Suit.Hearts, Rank.Three),
        });
        var io = new ScriptedGameIO(defaultResponse: "");
        var manager = new WARGameManager(io, playerHand, computerHand);

        manager.StartGame();

        Assert.Equal(4, manager.PlayerHandCount);
        Assert.Equal(0, manager.ComputerHandCount);
        Assert.Contains("You win!", io.AllOutput);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_StartGame_TieTriggersWarAndConservesCards()
    {
        var playerHand = new Queue<Card>(new[]
        {
            new Card(Suit.Hearts, Rank.Seven),
            new Card(Suit.Spades, Rank.Ace),
            new Card(Suit.Spades, Rank.Two),
            new Card(Suit.Spades, Rank.Three),
            new Card(Suit.Spades, Rank.Nine),
        });
        var computerHand = new Queue<Card>(new[]
        {
            new Card(Suit.Clubs, Rank.Seven),
            new Card(Suit.Clubs, Rank.Two),
            new Card(Suit.Clubs, Rank.Three),
            new Card(Suit.Clubs, Rank.Four),
            new Card(Suit.Clubs, Rank.Five),
        });
        var io = new ScriptedGameIO(defaultResponse: "");
        var manager = new WARGameManager(io, playerHand, computerHand);

        manager.StartGame();

        Assert.Contains("War!", io.AllOutput);
        Assert.Equal(10, manager.PlayerHandCount);
        Assert.Equal(0, manager.ComputerHandCount);
        Assert.Contains("You win!", io.AllOutput);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_StartGame_OpponentRunsOutOfCardsDuringWar_LosesImmediately()
    {
        var playerHand = new Queue<Card>(new[]
        {
            new Card(Suit.Hearts, Rank.Seven),
            new Card(Suit.Spades, Rank.Ace),
            new Card(Suit.Spades, Rank.King),
            new Card(Suit.Spades, Rank.Queen),
            new Card(Suit.Spades, Rank.Jack),
        });
        var computerHand = new Queue<Card>(new[]
        {
            new Card(Suit.Clubs, Rank.Seven),
        });
        var io = new ScriptedGameIO(defaultResponse: "");
        var manager = new WARGameManager(io, playerHand, computerHand);

        manager.StartGame();

        Assert.Contains("Computer ran out of cards during the war and loses the game!", io.AllOutput);
        Assert.Equal(6, manager.PlayerHandCount);
        Assert.Equal(0, manager.ComputerHandCount);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_StartGame_BothRunOutDuringWar_EndsInDraw()
    {
        var playerHand = new Queue<Card>(new[] { new Card(Suit.Hearts, Rank.Seven) });
        var computerHand = new Queue<Card>(new[] { new Card(Suit.Clubs, Rank.Seven) });
        var io = new ScriptedGameIO(defaultResponse: "");
        var manager = new WARGameManager(io, playerHand, computerHand);

        manager.StartGame();

        Assert.Contains("Both players ran out of cards at the same time - the game ends in a draw.", io.AllOutput);
        Assert.Equal(0, manager.PlayerHandCount);
        Assert.Equal(0, manager.ComputerHandCount);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_StartGame_RoundLimitReached_AnnouncesCardCountResult()
    {
        var playerHand = new Queue<Card>(Enumerable.Repeat(0, 4).Select(_ => new Card(Suit.Spades, Rank.Ace)));
        var computerHand = new Queue<Card>(Enumerable.Repeat(0, 4).Select(_ => new Card(Suit.Hearts, Rank.Two)));
        var io = new ScriptedGameIO(defaultResponse: "");
        var manager = new WARGameManager(io, playerHand, computerHand, maxRounds: 2);

        manager.StartGame();

        Assert.Contains("Round limit (2) reached without a natural winner.", io.AllOutput);
        Assert.True(manager.PlayerHandCount > 0);
        Assert.True(manager.ComputerHandCount > 0);
    }
}
