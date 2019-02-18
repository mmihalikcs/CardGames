using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace M2.CardGames.Common
{
    public enum Suits
    {
        [Description("S")]
        Spades,
        [Description("H")]
        Hearts,
        [Description("D")]
        Diamonds,
        [Description("C")]
        Clubs
    }

    public enum Colors
    {
        Red,
        Black
    }

    public enum Values
    {
        [Description("2")]
        Two,
        [Description("3")]
        Three,
        [Description("4")]
        Four,
        [Description("5")]
        Five,
        [Description("6")]
        Six,
        [Description("7")]
        Seven,
        [Description("8")]
        Eight,
        [Description("9")]
        Nine,
        [Description("10")]
        Ten,
        [Description("J")]
        Jack,
        [Description("Q")]
        Queen,
        [Description("K")]
        King,
        [Description("A")]
        Ace 
    }
}
