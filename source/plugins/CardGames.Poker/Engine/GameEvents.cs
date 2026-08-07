using CardGames.Domain.Interaction;
using CardGames.Domain.Models;

namespace CardGames.Poker.Engine;

internal sealed record SeatFolded(string SeatName) : GameEvent
{
    public override string Describe() => $"{SeatName} folds.";
}

internal sealed record SeatChecked(string SeatName) : GameEvent
{
    public override string Describe() => $"{SeatName} checks.";
}

internal sealed record SeatCalled(string SeatName, int Amount, bool AllIn) : GameEvent
{
    public override string Describe() => AllIn ? $"{SeatName} calls {Amount} and is all-in." : $"{SeatName} calls {Amount}.";
}

internal sealed record SeatRaised(string SeatName, int NewTotal, bool AllIn) : GameEvent
{
    public override string Describe() => AllIn ? $"{SeatName} raises to {NewTotal} and is all-in." : $"{SeatName} raises to {NewTotal}.";
}

internal sealed record BettingStatus(string SeatName, int Pot, int Chips, int ToCall) : GameEvent
{
    public override string Describe() => $"\nPot: {Pot} | Your chips: {Chips} | To call: {ToCall}";
}

internal sealed record InvalidActionRejected(string SeatName) : GameEvent
{
    public override string Describe() => "Invalid action for the current situation. Try again.";
}

internal sealed record InvalidSetupEntry : GameEvent
{
    public override string Describe() => "Invalid entry. Try again.";
}

internal sealed record HoleCardsRevealed(string SeatName, IReadOnlyList<Card> Cards) : GameEvent
{
    public override string Describe() => $"{SeatName}'s hole cards:{Environment.NewLine}{TableRenderer.RenderCardRow(Cards)}";
}

internal sealed record CommunityCardsRevealed(IReadOnlyList<Card> Cards) : GameEvent
{
    public override string Describe() => $"Community cards:{Environment.NewLine}{TableRenderer.RenderCardRow(Cards)}";
}

internal readonly record struct StackLine(string Name, int Chips, string Status);

internal sealed record StacksStatus(IReadOnlyList<StackLine> Lines) : GameEvent
{
    public override string Describe() => string.Join(Environment.NewLine, Lines.Select(l => $"{l.Name}: {l.Chips} chips ({l.Status})"));
}

internal sealed record HandShown(string SeatName, string Category) : GameEvent
{
    public override string Describe() => $"{SeatName} shows {Category}.";
}

internal sealed record PotAwarded(IReadOnlyList<string> WinnerNames, int Total, int SharePerWinner) : GameEvent
{
    public override string Describe() => WinnerNames.Count == 1
        ? $"{WinnerNames[0]} wins the pot of {Total}!"
        : $"Split pot! {string.Join(", ", WinnerNames)} each win {SharePerWinner}.";
}

internal sealed record NewHandStarted : GameEvent
{
    public override string Describe() => "\n=== New Hand ===";
}

internal sealed record UncontestedPotAwarded(string WinnerName, int Total) : GameEvent
{
    public override string Describe() => $"{WinnerName} wins the pot of {Total} uncontested!";
}

internal sealed record SessionEnded(string? WinnerName) : GameEvent
{
    public override string Describe() => WinnerName is null ? "\nSession ended." : $"\n{WinnerName} wins the session!";
}

internal sealed record DrawPhaseStarted : GameEvent
{
    public override string Describe() => "\n--- Draw Phase ---";
}

internal sealed record SeatStoodPat(string SeatName) : GameEvent
{
    public override string Describe() => $"{SeatName} stands pat.";
}

internal sealed record SeatDrew(string SeatName, int Count) : GameEvent
{
    public override string Describe() => $"{SeatName} draws {Count} card(s).";
}
