using CardGames.Common.Tests;
using CardGames.Domain.Enums;
using CardGames.Domain.Models;
using CardGames.Presentation.Extensions;
using Xunit;

namespace CardGames.Presentation.Tests.Extensions;

public sealed class VisualizationExtensionsTests
{
    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void GetEnumDescription_KnownValue_ReturnsDescription()
    {
        Assert.Equal("Hearts", Suit.Hearts.GetEnumDescription());
        Assert.Equal("Q", Rank.Queen.GetEnumDescription());
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void GetEnumDescription_UndefinedValue_FallsBackToToStringWithoutThrowing()
    {
        var undefinedSuit = (Suit)999;

        var exception = Record.Exception(() => undefinedSuit.GetEnumDescription());

        Assert.Null(exception);
        Assert.Equal("999", undefinedSuit.GetEnumDescription());
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void DisplayCard_ContainsRankAndSuitGlyph()
    {
        var card = new Card(Suit.Hearts, Rank.King);

        var display = card.DisplayCard();

        Assert.Contains("♥", display);
        Assert.Contains("K", display);
    }
}
