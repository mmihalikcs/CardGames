namespace CardGames.Domain.Models;

public class ApplicationSettings
{
    public string PluginDirectory { get; set; } = string.Empty;

    public bool MultiplayerEnabled { get; set; } = false;
}
