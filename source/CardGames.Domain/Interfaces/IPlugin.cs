namespace CardGames.Domain.Interfaces;

public interface IPlugin
{
    string Name { get; }

    string Description { get; }

    string Version { get; }
}
