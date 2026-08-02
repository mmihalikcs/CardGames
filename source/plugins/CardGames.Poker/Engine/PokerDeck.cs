using CardGames.Domain.Models;

namespace CardGames.Poker.Engine;

/// <summary>
/// A seedable card queue. DeckOfCards.ShuffleDeck() uses an internal, unseedable `new Random()`,
/// so this shuffles with an injected Random instead (same reasoning as GoFishGameManager's own
/// Random injection) to keep deals deterministic and testable.
/// </summary>
internal sealed class PokerDeck
{
    private readonly Queue<Card> _Cards;

    public PokerDeck(Random random)
    {
        var deck = new DeckOfCards();
        deck.InitializeStandardDeck(includeJokers: false);
        var cards = deck.CurrentDeck.ToList();
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
        _Cards = new Queue<Card>(cards);
    }

    // Test seam: exact deal order.
    internal PokerDeck(IEnumerable<Card> orderedCards)
    {
        _Cards = new Queue<Card>(orderedCards);
    }

    public int RemainingCount => _Cards.Count;

    public Card Draw() => _Cards.Count == 0
        ? throw new InvalidOperationException("PokerDeck is empty.")
        : _Cards.Dequeue();
}
