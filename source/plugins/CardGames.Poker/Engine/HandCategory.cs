namespace CardGames.Poker.Engine;

/// <summary>
/// Standard 9-category poker hand ranking, low to high. A royal flush is not a distinct
/// category - it's simply an ace-high StraightFlush, relevant only for display text.
/// </summary>
internal enum HandCategory
{
    HighCard = 0,
    OnePair = 1,
    TwoPair = 2,
    ThreeOfAKind = 3,
    Straight = 4,
    Flush = 5,
    FullHouse = 6,
    FourOfAKind = 7,
    StraightFlush = 8,
}
