using CardGames.Domain.Interaction;

namespace CardGames.GoFish.Tests.Fakes;

internal sealed class ScriptedGameChannel : IGameChannel
{
    private readonly Queue<string?> _ScriptedChoiceAnswers;
    private readonly string? _DefaultChoiceAnswer;

    public List<GameEvent> Published { get; } = new();

    public ScriptedGameChannel(IEnumerable<string?>? scriptedChoiceAnswers = null, string? defaultChoiceAnswer = null)
    {
        _ScriptedChoiceAnswers = new Queue<string?>(scriptedChoiceAnswers ?? Array.Empty<string?>());
        _DefaultChoiceAnswer = defaultChoiceAnswer;
    }

    public void Publish(GameEvent gameEvent) => Published.Add(gameEvent);

    // GoFish only ever awaits a ChoicePrompt (ask for a rank).
    public PromptResponse Await(GamePrompt prompt)
    {
        if (_ScriptedChoiceAnswers.Count > 0)
            return new ChoiceResponse(_ScriptedChoiceAnswers.Dequeue() ?? string.Empty);
        if (_DefaultChoiceAnswer != null)
            return new ChoiceResponse(_DefaultChoiceAnswer);
        throw new InvalidOperationException("ScriptedGameChannel.Await called with no scripted choice answers remaining.");
    }
}
