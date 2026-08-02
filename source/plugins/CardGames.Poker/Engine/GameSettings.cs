namespace CardGames.Poker.Engine;

internal static class GameSettings
{
    public const int StartingChips = 500;
    public const int AnteAmount = 10;
    public const int BetIncrement = 10;
    public const int MaxRaisesPerStreet = 3;
    public const int MinOpponents = 2;
    public const int MaxOpponents = 5;

    /// <summary>Safety cap on hands played per session, mirrors WAR's DefaultMaxRounds.</summary>
    public const int DefaultMaxHands = 500;
}
