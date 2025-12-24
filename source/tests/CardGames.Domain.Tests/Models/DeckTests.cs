using CardGames.Common.Tests;
using CardGames.Domain.Enums;
using CardGames.Domain.Models;
using Xunit;
using Xunit.Abstractions;

namespace CardGames.Domain.Tests.Models;

public sealed class DeckTests
{
    private readonly ITestOutputHelper _Output;

    public DeckTests(ITestOutputHelper output)
    { 
        _Output = output; 
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_CreateCardDeck()
    {
        // Empty Deck
        var deckOfCards = new DeckOfCards();
        Assert.True(deckOfCards.CurrentDeck.Count() == 0);
        // Initialize the deck
        deckOfCards.InitializeStandardDeck();
        Assert.True(deckOfCards.CurrentDeck.Count() == 52);
        // Print the deck
        foreach (var card in deckOfCards.CurrentDeck)
        {
            _Output.WriteLine($"[{card.Suit}]{card.Rank}");
        }
    }

    [Fact]
    public void Postive_CreateCardDeckWithJokers()
    {
        // Empty Deck
        var deckOfCards = new DeckOfCards();
        Assert.True(deckOfCards.CurrentDeck.Count() == 0);
        // Initialize the deck
        deckOfCards.InitializeStandardDeck(true);
        Assert.True(deckOfCards.CurrentDeck.Count() == 54);
        // Print the deck
        foreach (var card in deckOfCards.CurrentDeck)
        {
            _Output.WriteLine($"[{card.Suit}]{card.Rank}");
        }
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_CreateCardDeckAndAddMultiple()
    {
        // Empty Deck
        var deckOfCards = new DeckOfCards();
        Assert.True(deckOfCards.CurrentDeck.Count() == 0);
        // Initialize the deck
        deckOfCards.InitializeStandardDeck();
        Assert.True(deckOfCards.CurrentDeck.Count() == 52);
        // Add Card
        var cards = new List<Card>()
        {
            new Card(Suit.Diamonds, Rank.Three),
            new Card(Suit.Diamonds, Rank.Three),
            new Card(Suit.Diamonds, Rank.Three),
            new Card(Suit.Diamonds, Rank.Three)
        };
        deckOfCards.AddCards(cards);
        Assert.True(deckOfCards.CurrentDeck.Count() == 56);
        Assert.True(deckOfCards.CurrentDeck.Where(x => x.Rank == Rank.Three && x.Suit == Suit.Diamonds).Count() == 5);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_CreateCardDeckAndAdd()
    {
        // Empty Deck
        var deckOfCards = new DeckOfCards();
        Assert.True(deckOfCards.CurrentDeck.Count() == 0);
        // Initialize the deck
        deckOfCards.InitializeStandardDeck();
        Assert.True(deckOfCards.CurrentDeck.Count() == 52);
        // Add Card
        var card = new Card(Suit.Diamonds, Rank.Three);
        deckOfCards.AddCard(card);
        Assert.True(deckOfCards.CurrentDeck.Count() == 53);
        Assert.True(deckOfCards.CurrentDeck.Where(x => x.Rank == Rank.Three && x.Suit == Suit.Diamonds).Count() == 2);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_CreateCardDeckAndRemove()
    {
        // Empty Deck
        var deckOfCards = new DeckOfCards();
        Assert.True(deckOfCards.CurrentDeck.Count() == 0);
        // Initialize the deck
        deckOfCards.InitializeStandardDeck();
        Assert.True(deckOfCards.CurrentDeck.Count() == 52);
        // Add Card
        var cardToRemove = deckOfCards.CurrentDeck.Where(x => x.Rank == Rank.Three && x.Suit == Suit.Diamonds).FirstOrDefault();
        Assert.NotNull(cardToRemove);
        deckOfCards.RemoveCard(cardToRemove);
        Assert.True(deckOfCards.CurrentDeck.Count() == 51);
        Assert.DoesNotContain<Card>(cardToRemove, deckOfCards.CurrentDeck);
    }

    [Fact]
    [Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
    public void Postive_ShuffleDeck()
    {
        // Empty Deck
        var deckOfCards = new DeckOfCards();
        Assert.True(deckOfCards.CurrentDeck.Count() == 0);
        // Initialize the deck
        deckOfCards.InitializeStandardDeck();
        Assert.True(deckOfCards.CurrentDeck.Count() == 52);
        var previousOrder = new List<Card>(deckOfCards.CurrentDeck).AsReadOnly();
        deckOfCards.ShuffleDeck();
        Assert.False(previousOrder.SequenceEqual(deckOfCards.CurrentDeck));
    }
}
