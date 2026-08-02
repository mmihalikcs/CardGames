using CardGames.Common.Tests;
using CardGames.Domain.Enums;
using CardGames.Domain.Models;
using CardGames.GoFish.Tests.Fakes;
using Xunit;

namespace CardGames.GoFish.Tests;

public sealed class GoFishGameManagerTests
{
    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_StartGame_AskMatchCompletesBookAndAwardsExtraTurn()
    {
        var playerHand = new List<Card>
        {
            new Card(Suit.Hearts, Rank.Seven),
            new Card(Suit.Diamonds, Rank.Seven),
            new Card(Suit.Clubs, Rank.Seven),
            new Card(Suit.Hearts, Rank.Two),
        };
        var computerHand = new List<Card> { new Card(Suit.Spades, Rank.Seven) };
        var io = new ScriptedGameIO(new[] { "7" });
        var manager = new GoFishGameManager(io, new Random(1), playerHand, computerHand, new DeckOfCards());

        manager.StartGame();

        Assert.Equal(1, manager.PlayerBookCount);
        Assert.Equal(0, manager.ComputerBookCount);
        Assert.Equal(1, manager.PlayerHandCount);
        Assert.Equal(0, manager.ComputerHandCount);
        Assert.Contains("completed a book of 7s!", io.AllOutput);
        Assert.Contains("You win!", io.AllOutput);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_StartGame_GoFishNoMatch_DrawsNonMatchingCard()
    {
        var playerHand = new List<Card> { new Card(Suit.Hearts, Rank.Two) };
        var computerHand = new List<Card>();
        var drawPile = new DeckOfCards();
        drawPile.AddCard(new Card(Suit.Hearts, Rank.Five));
        var io = new ScriptedGameIO(new[] { "2" });
        var manager = new GoFishGameManager(io, new Random(1), playerHand, computerHand, drawPile);

        manager.StartGame();

        Assert.Contains("Go Fish!", io.AllOutput);
        Assert.Equal(2, manager.PlayerHandCount);
        Assert.Equal(0, manager.ComputerHandCount);
        Assert.Equal(0, manager.DrawPileCount);
        Assert.Contains("It's a draw!", io.AllOutput);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_StartGame_GoFishDrawMatchesAskedRank_GrantsExtraTurn()
    {
        var playerHand = new List<Card> { new Card(Suit.Hearts, Rank.Two) };
        var computerHand = new List<Card>();
        var drawPile = new DeckOfCards();
        drawPile.AddCard(new Card(Suit.Spades, Rank.Two));
        var io = new ScriptedGameIO(new[] { "2" });
        var manager = new GoFishGameManager(io, new Random(1), playerHand, computerHand, drawPile);

        manager.StartGame();

        Assert.Contains("go again!", io.AllOutput);
        Assert.Equal(2, manager.PlayerHandCount);
        Assert.Equal(0, manager.DrawPileCount);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_StartGame_GameAlreadyOver_AnnouncesWinnerWithoutTurns()
    {
        var playerHand = new List<Card>();
        var computerHand = new List<Card> { new Card(Suit.Hearts, Rank.Two) };
        var io = new ScriptedGameIO();
        var manager = new GoFishGameManager(io, new Random(1), playerHand, computerHand, new DeckOfCards(), playerBooks: 3, computerBooks: 1);

        manager.StartGame();

        Assert.DoesNotContain("Ask for a rank", io.AllOutput);
        Assert.Contains("You: 3 book(s), Computer: 1 book(s)", io.AllOutput);
        Assert.Contains("You win!", io.AllOutput);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Negative_StartGame_InvalidRankInput_Reprompts()
    {
        var playerHand = new List<Card> { new Card(Suit.Hearts, Rank.Two) };
        var computerHand = new List<Card>();
        var drawPile = new DeckOfCards();
        drawPile.AddCard(new Card(Suit.Hearts, Rank.Five));
        var io = new ScriptedGameIO(new[] { "9", "2" });
        var manager = new GoFishGameManager(io, new Random(1), playerHand, computerHand, drawPile);

        manager.StartGame();

        Assert.Contains("You can only ask for a rank you currently hold. Try again.", io.AllOutput);
        Assert.Contains("Go Fish!", io.AllOutput);
    }
}
