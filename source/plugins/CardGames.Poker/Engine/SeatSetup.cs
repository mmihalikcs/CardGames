using CardGames.Domain.Interaction;

namespace CardGames.Poker.Engine;

/// <summary>
/// Shared human/AI seat setup prompts for the poker game managers. Order is: human count, then
/// one name prompt per additional human (the local player is always seat 1, "You"), then AI
/// opponent count. This exact prompt sequence is mirrored by the networked lobby's setup-answer
/// queue (see CardGames.Networking) - keep both in sync if this changes. Prompts here are always
/// unscoped - no seat exists yet, so there's nothing to route to but the local/hosting player.
/// </summary>
internal static class SeatSetup
{
    private const string SetupSeatId = "You";

    public static void BuildSeats(IGameChannel io, List<Seat> seats)
    {
        int humanCount = PromptForCount(io, "How many human players (including you)?", GameSettings.MinHumans, GameSettings.MaxHumans);

        seats.Add(new Seat("You", isHuman: true, GameSettings.StartingChips));
        for (int i = 2; i <= humanCount; i++)
        {
            var response = (TextResponse)io.Await(new TextPrompt(SetupSeatId, $"Enter a name for player {i}: "));
            var name = response.Text;
            seats.Add(new Seat(string.IsNullOrWhiteSpace(name) ? $"Player {i}" : name.Trim(), isHuman: true, GameSettings.StartingChips));
        }

        int aiCount = PromptForCount(io, "How many AI opponents?", GameSettings.MinOpponents, GameSettings.MaxOpponents);
        for (int i = 1; i <= aiCount; i++)
            seats.Add(new Seat($"AI {i}", isHuman: false, GameSettings.StartingChips));
    }

    private static int PromptForCount(IGameChannel io, string message, int min, int max)
    {
        var validOptions = Enumerable.Range(min, max - min + 1).Select(n => n.ToString()).ToList();
        while (true)
        {
            var response = (ChoiceResponse)io.Await(new ChoicePrompt(SetupSeatId, message, validOptions));
            if (int.TryParse(response.OptionId, out var parsed) && parsed >= min && parsed <= max)
                return parsed;
            io.Publish(new InvalidSetupEntry());
        }
    }
}
