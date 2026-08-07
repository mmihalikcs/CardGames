namespace CardGames.Domain.Interaction;

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
}
