using CardGames.Networking.Bridge;

namespace CardGames.Networking.Tests.Fakes;

internal sealed class RecordingSeatChannel : ISeatChannel
{
    private readonly Queue<string?> _Input;

    public List<string> Output { get; } = new();

    public RecordingSeatChannel(params string?[] input)
    {
        _Input = new Queue<string?>(input);
    }

    public void Write(string message) => Output.Add(message);

    public void WriteLine(string message) => Output.Add(message);

    public string? ReadLine() => _Input.Count > 0
        ? _Input.Dequeue()
        : throw new InvalidOperationException("RecordingSeatChannel.ReadLine called with no scripted input remaining.");
}
