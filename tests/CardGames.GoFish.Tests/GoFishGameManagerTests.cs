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
        var channel = new ScriptedGameChannel(new[] { "7" });
        var manager = new GoFishGameManager(channel, new Random(1), playerHand, computerHand, new DeckOfCards());

        manager.StartGame();

        Assert.Equal(1, manager.PlayerBookCount);
        Assert.Equal(0, manager.ComputerBookCount);
        Assert.Equal(1, manager.PlayerHandCount);
        Assert.Equal(0, manager.ComputerHandCount);
        Assert.Contains(channel.Published, e => e is BookCompleted { SeatId: "player", RankLabel: "7" });
        Assert.Contains(channel.Published, e => e is GameEnded { WinnerSeatId: "player" });
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_StartGame_GoFishNoMatch_DrawsNonMatchingCard()
    {
        var playerHand = new List<Card> { new Card(Suit.Hearts, Rank.Two) };
        var computerHand = new List<Card>();
        var drawPile = new DeckOfCards();
        drawPile.AddCard(new Card(Suit.Hearts, Rank.Five));
        var channel = new ScriptedGameChannel(new[] { "2" });
        var manager = new GoFishGameManager(channel, new Random(1), playerHand, computerHand, drawPile);

        manager.StartGame();

        Assert.Contains(channel.Published, e => e is GoFishCalled { ResponderSeatId: "computer" });
        Assert.Equal(2, manager.PlayerHandCount);
        Assert.Equal(0, manager.ComputerHandCount);
        Assert.Equal(0, manager.DrawPileCount);
        Assert.Contains(channel.Published, e => e is GameEnded { WinnerSeatId: null });
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_StartGame_GoFishDrawMatchesAskedRank_GrantsExtraTurn()
    {
        var playerHand = new List<Card> { new Card(Suit.Hearts, Rank.Two) };
        var computerHand = new List<Card>();
        var drawPile = new DeckOfCards();
        drawPile.AddCard(new Card(Suit.Spades, Rank.Two));
        var channel = new ScriptedGameChannel(new[] { "2" });
        var manager = new GoFishGameManager(channel, new Random(1), playerHand, computerHand, drawPile);

        manager.StartGame();

        Assert.Contains(channel.Published, e => e is DrewAskedRank { SeatId: "player", RankLabel: "2" });
        Assert.Equal(2, manager.PlayerHandCount);
        Assert.Equal(0, manager.DrawPileCount);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_StartGame_GameAlreadyOver_AnnouncesWinnerWithoutTurns()
    {
        var playerHand = new List<Card>();
        var computerHand = new List<Card> { new Card(Suit.Hearts, Rank.Two) };
        var channel = new ScriptedGameChannel();
        var manager = new GoFishGameManager(channel, new Random(1), playerHand, computerHand, new DeckOfCards(), playerBooks: 3, computerBooks: 1);

        manager.StartGame();

        Assert.DoesNotContain(channel.Published, e => e is HandDisplayed);
        Assert.Contains(channel.Published, e => e is GameEnded { PlayerBooks: 3, ComputerBooks: 1, WinnerSeatId: "player" });
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Negative_StartGame_InvalidRankInput_Reprompts()
    {
        var playerHand = new List<Card> { new Card(Suit.Hearts, Rank.Two) };
        var computerHand = new List<Card>();
        var drawPile = new DeckOfCards();
        drawPile.AddCard(new Card(Suit.Hearts, Rank.Five));
        var channel = new ScriptedGameChannel(new[] { "9", "2" });
        var manager = new GoFishGameManager(channel, new Random(1), playerHand, computerHand, drawPile);

        manager.StartGame();

        Assert.Contains(channel.Published, e => e is RankAskRejected { SeatId: "player" });
        Assert.Contains(channel.Published, e => e is GoFishCalled { ResponderSeatId: "computer" });
    }
}
