using CardGames.Domain.Interaction;
using Godot;

namespace CardGames.Godot.Scripts;

/// <summary>
/// Bridges IGameChannel (blocking, called from the background Task running IGameManager.StartGame())
/// onto Godot's main-thread-only UI. Publish/Await marshal onto the main thread via
/// Callable.From(...).CallDeferred() - safe to call from any thread, and requires no Variant
/// marshaling of the closure's captured state, so GameEvent/GamePrompt can stay plain POCOs. Await
/// then blocks the calling background thread on a TaskCompletionSource&lt;PromptResponse&gt; that a UI
/// callback (a button Pressed handler, invoked on the main thread) resolves via SubmitPromptResponse.
/// Mirrors CardGames.Networking/Bridge/RemoteSeatChannel's TaskCompletionSource-blocking pattern, but
/// the "round trip" here is a deferred UI call instead of a SignalR request/response.
/// </summary>
public sealed class GodotGameChannel : ISeatContextGameChannel
{
    private readonly Action<GameEvent> _OnEventMainThread;
    private readonly Action<GamePrompt> _OnPromptMainThread;
    private TaskCompletionSource<PromptResponse>? _Pending;

    public GodotGameChannel(Action<GameEvent> onEventMainThread, Action<GamePrompt> onPromptMainThread)
    {
        _OnEventMainThread = onEventMainThread;
        _OnPromptMainThread = onPromptMainThread;
    }

    public void Publish(GameEvent gameEvent) =>
        Callable.From(() => _OnEventMainThread(gameEvent)).CallDeferred();

    // Safe to block on here because this always runs on the background Task driving StartGame()
    // (see MainController.StartGame), never on Godot's main/render thread - blocking there would
    // freeze the engine.
    public PromptResponse Await(GamePrompt prompt)
    {
        var tcs = new TaskCompletionSource<PromptResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _Pending = tcs;
        Callable.From(() => _OnPromptMainThread(prompt)).CallDeferred();
        return tcs.Task.GetAwaiter().GetResult();
    }

    /// <summary>Called from GameSessionPanel's widget callbacks, always already on the main thread.</summary>
    public void SubmitPromptResponse(PromptResponse response)
    {
        var tcs = _Pending;
        _Pending = null;
        tcs?.TrySetResult(response);
    }

    // No remote-seat routing needed for an embedded, single-window session - mirrors
    // TextGameChannel's no-op scope. Kept as ISeatContextGameChannel (not plain IGameChannel) so
    // this is a drop-in replacement anywhere ISeatContextGameChannel is required (e.g. Poker's
    // betting logic casts _Io to ISeatContextGameChannel).
    public IDisposable BeginParticipantScope(string participantId) => NoopScope.Instance;

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();
        public void Dispose() { }
    }
}
