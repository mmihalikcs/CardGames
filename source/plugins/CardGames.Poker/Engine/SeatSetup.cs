using CardGames.Domain.Interfaces;

namespace CardGames.Poker.Engine;

/// <summary>
/// Shared human/AI seat setup prompts for the poker game managers. Order is: human count, then
/// one name prompt per additional human (the local player is always seat 1, "You"), then AI
/// opponent count. This exact prompt sequence is mirrored by the networked lobby's setup-answer
/// queue (see CardGames.Networking) - keep both in sync if this changes.
/// </summary>
internal static class SeatSetup
{
    public static void BuildSeats(IGameIO io, List<Seat> seats)
    {
        int humanCount = PromptForCount(io, $"How many human players (including you)? ({GameSettings.MinHumans}-{GameSettings.MaxHumans}) ", GameSettings.MinHumans, GameSettings.MaxHumans);

        seats.Add(new Seat("You", isHuman: true, GameSettings.StartingChips));
        for (int i = 2; i <= humanCount; i++)
        {
            io.Write($"Enter a name for player {i}: ");
            var name = io.ReadLine();
            seats.Add(new Seat(string.IsNullOrWhiteSpace(name) ? $"Player {i}" : name.Trim(), isHuman: true, GameSettings.StartingChips));
        }

        int aiCount = PromptForCount(io, $"How many AI opponents ({GameSettings.MinOpponents}-{GameSettings.MaxOpponents})? ", GameSettings.MinOpponents, GameSettings.MaxOpponents);
        for (int i = 1; i <= aiCount; i++)
            seats.Add(new Seat($"AI {i}", isHuman: false, GameSettings.StartingChips));
    }

    private static int PromptForCount(IGameIO io, string prompt, int min, int max)
    {
        while (true)
        {
            io.Write(prompt);
            if (int.TryParse(io.ReadLine(), out var parsed) && parsed >= min && parsed <= max)
                return parsed;
            io.WriteLine("Invalid entry. Try again.");
        }
    }
}
