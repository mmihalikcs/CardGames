namespace CardGames.Domain.Interaction;

/// <summary>
/// A request for input from a specific seat, made through <see cref="IGamePromptChannel"/>. Unlike
/// <see cref="GameEvent"/>, the shape of a prompt is fully generic across plugins - every plugin's
/// input need reduces to one of these three kinds, so channels (console, network, test fakes) never
/// need plugin-specific knowledge to render or parse a prompt.
/// </summary>
public abstract record GamePrompt(string SeatId)
{
    public abstract string Describe();
}

/// <summary>A simple "press enter to continue" - no meaningful answer beyond acknowledgement.</summary>
public sealed record ConfirmPrompt(string SeatId, string Message) : GamePrompt(SeatId)
{
    public override string Describe() => Message;
}

/// <summary>
/// A choice constrained to <see cref="ValidOptions"/> (e.g. GoFish's held ranks, Poker's
/// fold/check/call/raise). ValidOptions is a rendering hint for the client (buttons instead of a
/// text field) - the engine that issued the prompt still validates the response itself and may
/// re-prompt, exactly like today's retry loops.
/// </summary>
public sealed record ChoicePrompt(string SeatId, string Message, IReadOnlyList<string> ValidOptions) : GamePrompt(SeatId)
{
    public override string Describe() => $"{Message} ({string.Join(", ", ValidOptions)}): ";
}

/// <summary>Free-form text input (player names, Poker's discard-position list) - the engine parses/validates it.</summary>
public sealed record TextPrompt(string SeatId, string Message) : GamePrompt(SeatId)
{
    public override string Describe() => Message;
}
