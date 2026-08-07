using CardGames.Domain.Enums;
using CardGames.Domain.Interaction;
using CardGames.Domain.Models;

namespace CardGames.GoFish;


internal static class Seats
{
    internal const string Player = "player";
    internal const string Computer = "computer";

    internal static string Subject(string seatId) => seatId == Player ? "You" : "Computer";
    internal static string Possessive(string seatId) => seatId == Player ? "Your" : "Computer's";
}

internal sealed record GameStarted : GameEvent
{
    public override string Describe() => "Welcome to Go Fish!";
}

internal sealed record HandDisplayed(string SeatId, IReadOnlyList<Card> Hand) : GameEvent
{
    public override string Describe() => $"\n{Seats.Possessive(SeatId)} hand:";

    public override IReadOnlyList<CardGroup> CardGroups => [new CardGroup(null, Hand)];
}

internal sealed record ComputerAnnouncedAsk(string RankLabel) : GameEvent
{
    public override string Describe() => $"Computer asks: Do you have any {RankLabel}s?";
}

internal sealed record HandReplenished(string SeatId) : GameEvent
{
    public override string Describe() => $"{Seats.Possessive(SeatId)} hand was empty - drew a card from the pile.";
}

internal sealed record RankMatched(string AskerSeatId, string ResponderSeatId, string RankLabel, int Count) : GameEvent
{
    public override string Describe() =>
        $"{Seats.Subject(ResponderSeatId)} had {Count} {RankLabel}(s)! {Seats.Subject(AskerSeatId)} took them.";
}

internal sealed record GoFishCalled(string ResponderSeatId) : GameEvent
{
    public override string Describe() => $"{Seats.Subject(ResponderSeatId)} says: Go Fish!";
}

internal sealed record DrawPileEmpty : GameEvent
{
    public override string Describe() => "The draw pile is empty.";
}

internal sealed record CardDrawn(string SeatId) : GameEvent
{
    public override string Describe() => $"{Seats.Subject(SeatId)} drew a card.";
}

internal sealed record DrewAskedRank(string SeatId, string RankLabel) : GameEvent
{
    public override string Describe() => $"{Seats.Subject(SeatId)} drew the {RankLabel} that was asked for - go again!";
}

internal sealed record BookCompleted(string SeatId, string RankLabel) : GameEvent
{
    public override string Describe() => $"{Seats.Subject(SeatId)} completed a book of {RankLabel}s!";
}

internal sealed record RankAskRejected(string SeatId, string Reason) : GameEvent
{
    public override string Describe() => Reason;
}

internal sealed record GameEnded(int PlayerBooks, int ComputerBooks, string? WinnerSeatId) : GameEvent
{
    public override string Describe()
    {
        var summary = $"\nGame over! You: {PlayerBooks} book(s), Computer: {ComputerBooks} book(s).";
        var outcome = WinnerSeatId switch
        {
            Seats.Player => "You win!",
            Seats.Computer => "Computer wins!",
            _ => "It's a draw!"
        };
        return $"{summary}\n{outcome}";
    }
}
