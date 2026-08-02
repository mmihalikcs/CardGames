namespace CardGames.Networking.Dtos;

public sealed record JoinResult(bool Success, string? ErrorMessage, string? HostPlayerName, IReadOnlyList<RosterEntry> Roster);
