using CardGames.Domain.Enums;
using CardGames.Domain.Models;
using Xunit;

namespace CardGames.Domain.Tests.Models;

public sealed class CardTests
{
    public CardTests() { }

    [Fact]
    public void Postive_CreateCard()
    {
        var card = new Card(Suit.Clubs, Rank.Queen);
        Assert.True(card.Suit.Equals(Suit.Clubs));
        Assert.True(card.Rank.Equals(Rank.Queen));
    }

    [Fact]
    public void Negative_CreateCardWithBadRank()
    {
        try
        {
            var card = new Card(Suit.Clubs, Rank.None);
        }
        catch (ArgumentException ex)
        {
            Assert.True(ex.Message?.ToLower() == "rank");
        }
    }

    [Fact]
    public void Negative_CreateCardWithBadSuit()
    {
        try
        {
            var card = new Card(Suit.None, Rank.Ten);
        }
        catch (ArgumentException ex)
        {
            Assert.True(ex.Message?.ToLower() == "suit");
        }
    }
}
