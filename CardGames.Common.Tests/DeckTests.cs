using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M2.CardGames.Common.Tests
{
    [TestClass]
    public class DeckTests
    {
        private DeckOfCards _Deck;

        [TestInitialize]
        public void DeckTestsInitalize()
        {
            _Deck = new DeckOfCards();
        }

        /// <summary>
        /// Testing if the peek test does not remove the card from the deck. 
        /// </summary>
        [TestMethod]
        public void PeekTest()
        {
            Console.WriteLine($"Deck Count: {_Deck.Count}");
            Assert.IsTrue(_Deck.Count == 52);
            var card = _Deck.Peek();
            Assert.IsNotNull(card);
            Console.WriteLine($"Deck Count after Peek(): {_Deck.Count}");
            Assert.IsTrue(_Deck.Count == 52);
        }

        /// <summary>
        /// Testing if the Draw removes the card from the deck
        /// </summary>
        [TestMethod]
        public void DrawTest()
        {
            // Draw off the first card
            var firstCard = _Deck.Draw();
            Assert.IsNotNull(firstCard);
            Console.WriteLine($"Drawn Card has a suit of {firstCard.Suit} and value of {firstCard.Value}");
            // Peek the second card
            var secondCard = _Deck.Peek();
            Assert.IsNotNull(secondCard);
            Console.WriteLine($"Drawn Card has a suit of {secondCard.Suit} and value of {secondCard.Value}");
            // Ensure the cards are not equal
            Assert.AreNotEqual(firstCard, secondCard);
        }

        /// <summary>
        /// Testing if the Draw(num) removes the number of cards from the deck
        /// </summary>
        [TestMethod]
        public void DrawMultipleTest()
        {
            const int numberOfCards = 5;
            // Initial Count of Deck
            var cardCountInitial = _Deck.Count;
            Console.WriteLine($"Initial Deck Count: {cardCountInitial}");
            // Count of Deck after cards drawn
            var cards = _Deck.Draw(numberOfCards);
            var cardCurrentCount = _Deck.Count;
            Console.WriteLine($"Initial Deck Count: {cardCurrentCount}");
            // Difference of values should equal number of cards drawn
            Assert.IsTrue(cardCountInitial - cardCurrentCount == numberOfCards);
            Console.WriteLine($"Number of cards drawn: {cardCountInitial - cardCurrentCount}");
            // Finally ensure drawn cards are not null
            foreach (var card in cards)
                Assert.IsNotNull(card);
        }

        [TestMethod]
        public void ShuffleTest()
        {
            _Deck.Shuffle();
        }
    }
}
