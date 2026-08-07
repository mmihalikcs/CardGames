using CardGames.Domain.Interaction;

namespace CardGames.Poker.Tests.Fakes;

internal sealed class ScriptedGameChannel : IGameChannel
{
    private readonly Queue<string?> _Input;
    private readonly string? _DefaultResponse;

    public List<GameEvent> Published { get; } = new();

    public ScriptedGameChannel(IEnumerable<string?>? input = null, string? defaultResponse = null)
    {
        _Input = new Queue<string?>(input ?? Array.Empty<string?>());
        _DefaultResponse = defaultResponse;
    }

    public void Publish(GameEvent gameEvent) => Published.Add(gameEvent);

    public PromptResponse Await(GamePrompt prompt)
    {
        var raw = NextRaw() ?? string.Empty;
        return prompt switch
        {
            ConfirmPrompt => new Ack(),
            ChoicePrompt => new ChoiceResponse(raw),
            TextPrompt => new TextResponse(raw),
            _ => throw new NotSupportedException(prompt.GetType().Name)
        };
    }

    private string? NextRaw()
    {
        if (_Input.Count > 0)
            return _Input.Dequeue();
        if (_DefaultResponse != null)
            return _DefaultResponse;
        throw new InvalidOperationException("ScriptedGameChannel.Await called with no scripted input remaining.");
    }
}
