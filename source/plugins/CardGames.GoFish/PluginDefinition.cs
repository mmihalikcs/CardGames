using CardGames.Domain.Interfaces;

namespace CardGames.GoFish;

public sealed class GoFishPlugin : IPlugin
{
    public string Name => "Go Fish";

    public string Description => "Ask your opponent for a rank, go fish if they don't have it, and collect books of four!";

    public string Version => "1.0.0";

    public IGameManager CreateGameManager(IGameIO io) => new GoFishGameManager(io);
}
