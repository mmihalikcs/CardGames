using CardGames.Domain.Interfaces;

namespace CardGames.Console.Services;

public sealed class ConsoleGameIO : IGameIO
{
    public void Write(string message) => System.Console.Write(message);

    public void WriteLine(string message = "") => System.Console.WriteLine(message);

    public string? ReadLine() => System.Console.ReadLine();
}
