using CardGames.Domain.Models;

namespace CardGames.Poker.Engine;

internal sealed class Seat
{
    public string Name { get; }

    public bool IsHuman { get; }

    public int Chips { get; set; }

    public List<Card> HoleCards { get; } = new();

    public bool HasFolded { get; set; }

    public bool IsAllIn { get; set; }

    public int CommittedThisStreet { get; set; }

    /// <summary>Still eligible to win the pot (hasn't folded).</summary>
    public bool IsInHand => !HasFolded;

    /// <summary>Can still take a betting action this street (in the hand and not all-in).</summary>
    public bool IsActive => !HasFolded && !IsAllIn;

    public Seat(string name, bool isHuman, int startingChips)
    {
        Name = name;
        IsHuman = isHuman;
        Chips = startingChips;
    }
}
