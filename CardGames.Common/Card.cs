using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace M2.CardGames.Common
{
    public sealed class Card
    {
        private Suits _Suit;
        private Values _Value;

        public Card (Suits cardSuit, Values cardValues)
        {
            _Suit = cardSuit;
            _Value = cardValues;
        }

        public Suits Suit => _Suit;
        public Values Value => _Value;
    }

    public sealed class DeckOfCards
    {
        private List<Card> _Deck;
        private Random _Random;

        public DeckOfCards()
        {
            _Deck = LoadDeck();
            _Random = new Random();
        }

        public int Count => _Deck.Count();

        #region Public Methods
        /// <summary>
        /// Randomly shuffles the deck
        /// Fisher-Yates algorithm
        /// </summary>
        public void Shuffle()
        {
            for (var i = 0; i < _Deck.Count; i++)
                Swap(i, _Random.Next(i, _Deck.Count));
        }

        /// <summary>
        /// Peeks the top card on the deck.
        /// Does not remove.
        /// </summary>
        /// <returns></returns>
        public Card Peek()
        {
            return _Deck.Count > 0 ? _Deck[0] : null;
        }

        /// <summary>
        /// Draws a single card from the top of the deck.
        /// Removes the card from the deck.
        /// </summary>
        /// <returns></returns>
        public Card Draw()
        {
            return Draw(1).First();
        }

        /// <summary>
        /// Draws n cards from the top of the deck.
        /// Removes the cards from the deck.
        /// </summary>
        /// <param name="numberOfCards"></param>
        /// <returns></returns>
        public List<Card> Draw(int numberOfCards)
        {
            List<Card> cards = new List<Card>();
            if (_Deck.Count > 0 && _Deck.Count >= numberOfCards)
            {
                // Get the list of cards
                for (int i = 0; i < numberOfCards; i++)
                    cards.Add(_Deck[i]);
                // Finally remove the range
                _Deck.RemoveRange(0, numberOfCards);
                return cards;
            }
            return null;
        }
        #endregion

        #region Private Helpers
        /// <summary>
        /// Standard deck of cards has 52 cards. 
        /// 2-10, Jack, Queen, King, Ace
        /// </summary>
        /// <returns></returns>
        List<Card> LoadDeck()
        {
            List<Card> tmpDeck = new List<Card>();
            // Add Suite Ranges
            foreach (var suit in Enum.GetValues(typeof(Suits)).Cast<Suits>())
                tmpDeck.AddRange(Enumerable.Range(0, 13).Select(i => new Card(suit, (Values)i)));
            return tmpDeck;
        }

        void Swap(int i, int j)
        {
            var temp = _Deck[i];
            _Deck[i] = _Deck[j];
            _Deck[j] = temp;
        }
        #endregion
    }
}
