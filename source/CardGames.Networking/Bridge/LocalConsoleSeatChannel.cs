namespace CardGames.Networking.Bridge;

/// <summary>The host's own seat - direct console I/O, no network hop.</summary>
internal sealed class LocalConsoleSeatChannel : ISeatChannel
{
    public void Write(string message) => Console.Write(message);

    public void WriteLine(string message) => Console.WriteLine(message);

    public string? ReadLine() => Console.ReadLine();
}
