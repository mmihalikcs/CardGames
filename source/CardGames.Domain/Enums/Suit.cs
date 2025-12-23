using System.ComponentModel;

namespace CardGames.Domain.Enums;

public enum Suit
{
    [Description("?")]
    None,
    [Description("Hearts")]
    Hearts,
    [Description("Diamonds")]
    Diamonds,
    [Description("Clubs")]
    Clubs,
    [Description("Spades")]
    Spades
}