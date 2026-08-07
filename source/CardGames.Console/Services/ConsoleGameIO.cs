using CardGames.Domain.Interfaces;

namespace CardGames.Presentation.Services;

public sealed class ConsoleGameIO : IGameIO
{
    public void Write(string message) => Console.Write(message);

    public void WriteLine(string message = "") => Console.WriteLine(message);

    public string? ReadLine() => Console.ReadLine();
}
