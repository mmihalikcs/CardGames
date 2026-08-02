using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;

namespace CardGames.Poker;

public sealed class PokerPlugin : IPlugin
{
    public string Name => "Poker";

    public string Description => "Play Texas Hold'em, Omaha, or Five-Card Draw against configurable AI opponents.";

    public string Version => "1.0.0";

    public IReadOnlyList<GameVariant> Variants => PokerVariants.All;

    public IGameManager CreateGameManager(IGameIO io) => CreateGameManager(io, PokerVariants.TexasHoldem);

    public IGameManager CreateGameManager(IGameIO io, GameVariant variant) => variant.Key switch
    {
        PokerVariants.TexasHoldemKey => new TexasHoldemGameManager(io),
        PokerVariants.OmahaKey => new OmahaGameManager(io),
        PokerVariants.FiveCardDrawKey => new FiveCardDrawGameManager(io),
        _ => throw new ArgumentException($"Unknown poker variant '{variant.Key}'.", nameof(variant)),
    };
}
