using CardGames.Domain.Enums;

namespace CardGames.Domain.Models;

public class Card
{
    public Suit Suit
    {
        get;
        init
        {
            if (value == Suit.None)
            {
                throw new ArgumentException(nameof(Suit));
            }
            else
            {
                field = value;
            }
        }
    }

    public Rank Rank { get; init; }

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public override string ToString() => $"{Rank} of {Suit}";
}