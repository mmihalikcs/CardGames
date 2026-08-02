namespace CardGames.Networking.Sessions;

internal sealed record PlayerSlot(string ConnectionId, string PlayerName, int JoinOrder);
