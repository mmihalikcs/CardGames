using CardGames.Domain.Interaction;

namespace CardGames.WAR.Tests.Fakes;

internal sealed class ScriptedGameChannel : IGameChannel
{
    public List<GameEvent> Published { get; } = new();

    public void Publish(GameEvent gameEvent) => Published.Add(gameEvent);

    // WAR only ever awaits a ConfirmPrompt (press Enter to continue) - always acknowledge.
    public PromptResponse Await(GamePrompt prompt) => new Ack();
}
