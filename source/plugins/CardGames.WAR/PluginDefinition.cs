using CardGames.Domain.Interfaces;

namespace CardGames.WAR;

public sealed class WARPlugin : IPlugin
{
    public string Name => "WAR!";

    public string Description => "Classic WAR: flip cards, highest rank wins the pile - ties trigger a war!";

    public string Version => "1.0.0";

    public IGameManager CreateGameManager(IGameIO io) => new WARGameManager(io);
}
