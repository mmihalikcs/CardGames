using CardGames.Domain.Enums;

namespace CardGames.Domain.Models;

public class Card
{
    public Suit Suit { get; init; }

    public Rank Rank { get; init; }

    public Card(Suit suit, Rank rank)
    {
        if (rank == Rank.None)
            throw new ArgumentException(nameof(rank));
        if (suit == Suit.None && rank != Rank.Joker)
            throw new ArgumentException(nameof(suit));

        // Set the values
        Suit = suit;
        Rank = rank;
    }

    public override string ToString() => $"{Rank} of {Suit}";
}