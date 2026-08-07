using CardGames.Domain.Interaction;
using CardGames.Domain.Models;

namespace CardGames.Domain.Interfaces;

public interface IPlugin
{
    string Name { get; }

    string Description { get; }

    string Version { get; }

    IGameManager CreateGameManager(IGameChannel io);

    /// <summary>
    /// Optional selectable variants/modes this plugin offers. Plugins that only ever offer a
    /// single game leave this at its default empty list, and the presentation layer calls
    /// CreateGameManager(IGameChannel) directly with no variant prompt.
    /// </summary>
    IReadOnlyList<GameVariant> Variants => Array.Empty<GameVariant>();

    /// <summary>
    /// Creates a game manager for a specific variant returned by <see cref="Variants"/>. The
    /// default implementation ignores the variant and delegates to CreateGameManager(IGameChannel),
    /// so existing plugins with no variants need zero source changes.
    /// </summary>
    IGameManager CreateGameManager(IGameChannel io, GameVariant variant) => CreateGameManager(io);

    /// <summary>
    /// Whether this plugin can be played over the network (multiple human seats routed to
    /// distinct remote connections via ISeatContextGameChannel) rather than only local hot-seat.
    /// Defaults to false so existing plugins need zero source changes.
    /// </summary>
    bool SupportsMultiplayer => false;
}
