namespace CardGames.Networking.Bridge;

/// <summary>One participant's I/O channel: the host's own console, or one remote connection.</summary>
internal interface ISeatChannel
{
    void Write(string message);

    void WriteLine(string message);

    string? ReadLine();
}
