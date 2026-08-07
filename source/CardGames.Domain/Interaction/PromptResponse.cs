namespace CardGames.Domain.Interaction;

/// <summary>A seat's answer to a <see cref="GamePrompt"/>. One concrete shape per prompt kind.</summary>
public abstract record PromptResponse;

/// <summary>Answers a <see cref="ConfirmPrompt"/> - acknowledgement carries no data.</summary>
public sealed record Ack : PromptResponse;

/// <summary>Answers a <see cref="ChoicePrompt"/>. The issuing engine validates OptionId itself.</summary>
public sealed record ChoiceResponse(string OptionId) : PromptResponse;

/// <summary>Answers a <see cref="TextPrompt"/>.</summary>
public sealed record TextResponse(string Text) : PromptResponse;
