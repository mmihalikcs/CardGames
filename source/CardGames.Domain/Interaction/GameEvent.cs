using CardGames.Domain.Models;

namespace CardGames.Domain.Interaction;

/// <summary>
/// Where a CardGroup belongs on a structured client's table layout - a participant's own cards
/// (default) vs. a shared/table area everyone can see (e.g. Poker's community board). A generic
/// placement hint, not plugin-specific: a client like Godot can lay out "Own" near the local seat
/// and "Community" at the center of the table without knowing which plugin published the event.
/// </summary>
public enum CardGroupRole
{
    Own,
    Community
}

/// <summary>
/// A labeled set of cards significant to a GameEvent - e.g. WAR's two revealed cards, Poker's hole/
/// community cards, GoFish's hand. Label is a rendering hint (e.g. "You", "Community") a client can
/// show above the group; null for an unlabeled row. Role hints where the group belongs on a table
/// layout (see <see cref="CardGroupRole"/>).
/// </summary>
public sealed record CardGroup(string? Label, IReadOnlyList<Card> Cards, CardGroupRole Role = CardGroupRole.Own);

/// <summary>
/// A fact a game manager reports through <see cref="IGameEventSink"/> - "what happened", past
/// tense. Plugins define their own leaf event types (e.g. WAR's PileAwarded, Poker's SeatFolded)
/// and implement <see cref="Describe"/> for the text renderers (<see cref="TextGameChannel"/>);
/// a structured client (e.g. a future Godot front end) can instead pattern-match on the concrete
/// event type and ignore Describe() entirely.
/// </summary>
public abstract record GameEvent
{
    public abstract string Describe();

    /// <summary>
    /// Cards visually significant to this event, if any (a reveal, a hand, a community board) -
    /// empty by default. TextGameChannel renders these as ASCII art appended after Describe(); a
    /// structured client (e.g. Godot) can instead draw real card graphics from the same data,
    /// without needing compile-time knowledge of the plugin's concrete event type - unlike
    /// GameEvent's leaf types, CardGroup is defined once in Domain and shared by every plugin.
    /// </summary>
    public virtual IReadOnlyList<CardGroup> CardGroups => [];
}
