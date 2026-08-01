using CardGames.Domain.Interfaces;

namespace CardGames.GoFish;

public sealed class GoFishPlugin : IPlugin
{
    public string Name => "Go Fish";

    public string Description => "A Description of the game goes here!";

    public string Version => "1.0.0";
}
