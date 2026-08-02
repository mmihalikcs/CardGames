namespace CardGames.Domain.Interfaces;

public interface IGameIO
{
    void Write(string message);

    void WriteLine(string message = "");

    string? ReadLine();
}
