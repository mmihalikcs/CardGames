using CardGames.Application.Services;
using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;
using Godot;
using Microsoft.Extensions.Logging;

namespace CardGames.Godot.Scripts;

/// <summary>
/// Composition root for the embedded, single-player Godot client. Instantiates
/// AssemblyLoaderService/SettingsService directly (no Microsoft.Extensions.Hosting - there's a
/// single consumer, so a generic host adds ceremony with no payoff), discovers plugins the same way
/// CardGames.Console's Load Game menu does, and switches between the plugin-select and game-session
/// screens. IGameManager.StartGame() is fully synchronous/blocking, so it always runs on a
/// background Task - never on Godot's main thread - mirroring GameSessionManager.StartSessionAsync's
/// precedent for the same reason.
/// </summary>
public partial class MainController : Control
{
    private PluginSelectPanel _PluginSelectPanel = null!;
    private GameSessionPanel _GameSessionPanel = null!;
    private IAssemblyLoaderService _AssemblyLoaderService = null!;

    public override void _Ready()
    {
        _PluginSelectPanel = GetNode<PluginSelectPanel>("PluginSelectPanel");
        _GameSessionPanel = GetNode<GameSessionPanel>("GameSessionPanel");

        _PluginSelectPanel.Visible = true;
        _GameSessionPanel.Visible = false;

        _PluginSelectPanel.PluginChosen += OnPluginChosen;
        _GameSessionPanel.BackToMenuRequested += ReturnToMenu;

        var loggerFactory = LoggerFactory.Create(builder => { });
        _AssemblyLoaderService = new AssemblyLoaderService(loggerFactory);
        var settingsService = new SettingsService(loggerFactory);

        var settings = settingsService.Load();
        var pluginDirectory = string.IsNullOrWhiteSpace(settings.PluginDirectory)
            ? AppContext.BaseDirectory
            : settings.PluginDirectory;

        var plugins = Directory.Exists(pluginDirectory)
            ? _AssemblyLoaderService.DiscoverPlugins(pluginDirectory)
            : Array.Empty<IPlugin>();

        GD.Print($"CardGames.Godot: discovered {plugins.Count} plugin(s) in '{pluginDirectory}'.");
        _PluginSelectPanel.ShowPlugins(plugins, pluginDirectory);
    }

    private void OnPluginChosen(IPlugin plugin, GameVariant? variant)
    {
        var channel = new GodotGameChannel(_GameSessionPanel.AppendEvent, _GameSessionPanel.ShowPrompt);
        _GameSessionPanel.AttachChannel(channel);
        _GameSessionPanel.ResetForNewSession();

        _PluginSelectPanel.Visible = false;
        _GameSessionPanel.Visible = true;

        var gameManager = variant is null ? plugin.CreateGameManager(channel) : plugin.CreateGameManager(channel, variant);

        _ = Task.Run(() =>
        {
            var cancelled = false;
            try
            {
                gameManager.StartGame();
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            finally
            {
                if (!cancelled)
                    Callable.From(_GameSessionPanel.ShowGameOver).CallDeferred();
            }
        });
    }

    private void ReturnToMenu()
    {
        _GameSessionPanel.Visible = false;
        _PluginSelectPanel.Visible = true;
    }
}
