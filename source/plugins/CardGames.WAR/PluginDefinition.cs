using CardGames.Domain.Interfaces;

namespace CardGames.WAR;

public sealed class WARPlugin : IPlugin
{
    public string Name => "WAR!";

    public string Description => "A Description of the game goes here!";

    public string Version => "1.0.0";
}
