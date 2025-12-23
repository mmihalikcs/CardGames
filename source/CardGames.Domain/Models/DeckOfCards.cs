using CardGames.Domain.Enums;

namespace CardGames.Domain.Models;

public sealed class DeckOfCards
{
    // Properties
    public IReadOnlyList<Card> CurrentDeck => Cards.AsReadOnly();

    // Field
    private List<Card> Cards;

    public DeckOfCards()
    {
        Cards = new List<Card>();
    }

    /// <summary>
    /// Instantiates a new deck of cards with all suits and ranks. Optional bool to include jokers
    /// </summary>
    public void InitializeStandardDeck(bool includeJokers = false)
    {
        // Base Rank Query
        var ranks = Enum.GetValues<Rank>().Where(x => x != Rank.None && x != Rank.Joker);
        var suits = Enum.GetValues<Suit>().Where(x => x != Suit.None).ToList();

        // Initialize the decks base on the options
        foreach (var suit in suits)
        {
            foreach (var rank in ranks)
            {
                Cards.Add(new Card(suit, rank));
            }
        }

        // Add Two Jokers if jokers are enabled
        if (includeJokers)
        {
            Cards.Add(new Card(Suit.None, Rank.Joker));
        }
    }

    /// <summary>
    /// Adds a single cards to the deck
    /// </summary>
    /// <param name="card"></param>
    public void AddCard(Card card)
    {
        Cards.Add(card);
    }

    /// <summary>
    /// Adds an IEnumerable of cards to the current deck
    /// </summary>
    /// <param name="card"></param>
    public void AddCards(IEnumerable<Card> card)
    {
        Cards.AddRange(card);
    }

    /// <summary>
    /// Removes the single card from the deck by reference
    /// </summary>
    /// <param name="card"></param>
    /// <returns></returns>
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