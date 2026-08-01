using CardGames.Common.Tests;
using CardGames.Domain.Enums;
using CardGames.Domain.Models;
using Xunit;

namespace CardGames.Domain.Tests.Models;

public sealed class CardTests
{
    public CardTests() { }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_CreateCard()
    {
        var card = new Card(Suit.Clubs, Rank.Queen);
        Assert.True(card.Suit.Equals(Suit.Clubs));
        Assert.True(card.Rank.Equals(Rank.Queen));
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Negative_CreateCardWithBadRank_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Card(Suit.Clubs, Rank.None));
        Assert.Equal("rank", ex.Message);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Negative_CreateCardWithBadSuit_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Card(Suit.None, Rank.Ten));
        Assert.Equal("suit", ex.Message);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_CreateJokerWithNoSuit()
    {
        var card = new Card(Suit.None, Rank.Joker);
        Assert.Equal(Suit.None, card.Suit);
        Assert.Equal(Rank.Joker, card.Rank);
    }
}
