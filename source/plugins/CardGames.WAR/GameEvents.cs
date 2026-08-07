using CardGames.Domain.Interaction;
using CardGames.Domain.Models;

namespace CardGames.WAR;

internal static class Seats
{
    internal const string Player = "player";
    internal const string Computer = "computer";
}

internal sealed record GameStarted : GameEvent
{
    public override string Describe() => "Welcome to WAR! Press Enter after each round to continue.";
}

/// <summary>Reason is "Flip" for a normal round or "War" for a war-cards reveal - same shape, different prefix.</summary>
internal sealed record CardsRevealed(Card PlayerCard, Card ComputerCard, string Reason) : GameEvent
{
    public override string Describe()
    {
        var prefix = Reason == "War" ? "War cards: " : string.Empty;
        return $"{prefix}You played {PlayerCard}. Computer played {ComputerCard}.";
    }

    public override IReadOnlyList<CardGroup> CardGroups =>
        [new CardGroup("You", [PlayerCard]), new CardGroup("Computer", [ComputerCard])];
}

internal sealed record WarTriggered(bool Repeat = false) : GameEvent
{
    public override string Describe() => Repeat ? "War again!" : "War!";
}

internal sealed record PileAwarded(string WinnerSeatId, int CardCount) : GameEvent
{
    public override string Describe() => $"{(WinnerSeatId == Seats.Player ? "You" : "Computer")} won the pile of {CardCount} card(s).";
}

internal sealed record RoundLimitReached(int MaxRounds) : GameEvent
{
    public override string Describe() => $"\nRound limit ({MaxRounds}) reached without a natural winner.";
}

/// <summary>
/// Covers every way a game of WAR can end. Reason mirrors the original prose branches:
/// OpponentRanOutDuringWar/MutualRunOutDuringWar (mid-war), RanOutOfCards/MutualRunOut
/// (post-round-loop), RoundLimitReached (safety cap hit, winner by card count or a draw).
/// </summary>
internal sealed record GameEnded(string? WinnerSeatId, string Reason) : GameEvent
{
    public override string Describe() => Reason switch
    {
        "OpponentRanOutDuringWar" when WinnerSeatId == Seats.Player =>
            "Computer ran out of cards during the war and loses the game!",
        "OpponentRanOutDuringWar" =>
            "You ran out of cards during the war and lose the game!",
        "MutualRunOutDuringWar" =>
            "Both players ran out of cards at the same time - the game ends in a draw.",
        "MutualRunOut" =>
            "\nThe game ended in a draw - both players ran out of cards at once.",
        "RanOutOfCards" when WinnerSeatId == Seats.Player =>
            "\nYou win! Computer ran out of cards.",
        "RanOutOfCards" =>
            "\nComputer wins! You ran out of cards.",
        "RoundLimitReached" when WinnerSeatId is null =>
            "It's a draw!",
        "RoundLimitReached" when WinnerSeatId == Seats.Player =>
            "You win on card count!",
        "RoundLimitReached" =>
            "Computer wins on card count!",
        _ => throw new NotSupportedException($"Unknown GameEnded reason '{Reason}'.")
    };
}
