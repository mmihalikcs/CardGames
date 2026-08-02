using CardGames.Domain.Interfaces;

namespace CardGames.GoFish.Tests.Fakes;

internal sealed class ScriptedGameIO : IGameIO
{
    private readonly Queue<string?> _Input;
    private readonly string? _DefaultResponse;

    public List<string> Output { get; } = new();

    public ScriptedGameIO(IEnumerable<string?>? input = null, string? defaultResponse = null)
    {
        _Input = new Queue<string?>(input ?? Array.Empty<string?>());
        _DefaultResponse = defaultResponse;
    }

    public void Write(string message) => Output.Add(message);

    public void WriteLine(string message = "") => Output.Add(message);

    public string? ReadLine()
    {
        if (_Input.Count > 0)
            return _Input.Dequeue();
        if (_DefaultResponse != null)
            return _DefaultResponse;
        throw new InvalidOperationException("ScriptedGameIO.ReadLine called with no scripted input remaining.");
    }

    public string AllOutput => string.Join("\n", Output);
}
