namespace CardGames.Poker.Engine;

internal sealed class Pot
{
    public int Total { get; private set; }

    public void Add(int amount) => Total += amount;
}
