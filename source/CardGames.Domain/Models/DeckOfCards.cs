using CardGames.Domain.Enums;

namespace CardGames.Domain.Models;

public sealed class DeckOfCards
{
    public List<Card> Cards { get; }

    public DeckOfCards()
    {
        Cards = new List<Card>();
    }

    // Method to initialize a standard deck of 52 cards
    public void InitializeStandardDeck()
    {
        foreach (var suit in Enum.GetValues<Suit>())
        {
            foreach (var rank in Enum.GetValues<Rank>())
            {
                Cards.Add(new Card(suit, rank));
            }
        }
    }

    // Method to add a card to the deck
    public void AddCard(Card card)
    {
        Cards.Add(card);
    }
    
    // Method to remove a card from the deck
    public bool RemoveCard(Card card)
    {
        return Cards.Remove(card);
    }

    // Method to shuffle the deck
    public void Shuffle()
    {
        var random = new Random();
        int n = Cards.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var temp = Cards[i];
            Cards[i] = Cards[j];
            Cards[j] = temp;
        }
    }
}