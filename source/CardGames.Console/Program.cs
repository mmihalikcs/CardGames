using CardGames.Application.Services;
using CardGames.Domain.Interaction;
using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;
using CardGames.Networking.Client;
using CardGames.Networking.Dtos;
using CardGames.Networking.Hosting;
using CardGames.Networking.Sessions;
using CardGames.Console.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

// Must match CardGames.Poker.Engine.GameSettings' bounds (MinHumans=1/MaxHumans=4,
// MinOpponents=2/MaxOpponents=5) - the networked lobby answers the setup-answer queue up front
// with no live retry, so an out-of-range value here would desync SeatSetup.BuildSeats' prompts.
const int MinTotalHumanPlayers = 2; // at least 1 remote player beyond the host
const int MaxTotalHumanPlayers = 4;
const int MinAiOpponents = 2;
const int MaxAiOpponents = 5;

// Generic Host Creation
using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        //services.AddLogging(configure => configure.AddConsole());

        // DI
        services.AddSingleton<IAssemblyLoaderService, AssemblyLoaderService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IGameIO, ConsoleGameIO>();
        services.AddSingleton<ConsoleRenderer>();
        // Cheap to register unconditionally - constructing it binds no socket. The actual
        // Kestrel/SignalR listener only ever starts inside GameServerHost.StartAsync, which is
        // only reachable from a code path gated by ApplicationSettings.MultiplayerEnabled.
        services.AddSingleton<IGameSessionManager, GameSessionManager>();
    })
    .Build();

// Run Block
try
{
    // Run the host
    await host.StartAsync();

    // Set console output encoding to UTF-8
    Console.OutputEncoding = Encoding.UTF8;

    // Get All Services
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var assemblyLoaderService = host.Services.GetRequiredService<IAssemblyLoaderService>();
    var settingsService = host.Services.GetRequiredService<ISettingsService>();
    var gameIo = host.Services.GetRequiredService<IGameIO>();
    var consoleRenderer = host.Services.GetRequiredService<ConsoleRenderer>();
    var sessionManager = host.Services.GetRequiredService<IGameSessionManager>();

    // Main Loop
    IPlugin? loadedPlugin = null;
    var settings = settingsService.Load();
    int selection = -1;
    while (selection != 0)
    {
        consoleRenderer.DisplayMenu();
        if (!int.TryParse(Console.ReadLine(), out int result) || !consoleRenderer.GetCommands().ContainsKey(result))
        {
            Console.WriteLine("\nInvalid Entry! Try Again.\n");
            continue;
        }
        logger.LogDebug("Parsed entry: {Selection}", result);
        selection = result;

        switch (selection)
        {
            case 1: // Play
                if (loadedPlugin == null)
                {
                    Console.WriteLine("\nNo game is loaded. Use 'Load Game' to choose one first.\n");
                    break;
                }

                bool hostMultiplayer = false;
                bool joinMultiplayer = false;
                if (settings.MultiplayerEnabled && loadedPlugin.SupportsMultiplayer)
                {
                    consoleRenderer.DisplaySubmenu(
                        "Choose how to play",
                        new[] { "Single Player", "Host Multiplayer Game", "Join Multiplayer Game" });
                    if (!int.TryParse(Console.ReadLine(), out int modeChoice) || modeChoice < 0 || modeChoice > 3)
                    {
                        Console.WriteLine("\nInvalid selection.\n");
                        break;
                    }
                    if (modeChoice == 0)
                    {
                        Console.WriteLine("\nCancelled.\n");
                        break;
                    }
                    hostMultiplayer = modeChoice == 2;
                    joinMultiplayer = modeChoice == 3;
                }

                if (joinMultiplayer)
                {
                    await JoinMultiplayerGameAsync(loadedPlugin);
                    break;
                }

                var variants = loadedPlugin.Variants;
                GameVariant? selectedVariant = null;
                if (variants.Count > 0)
                {
                    consoleRenderer.DisplaySubmenu(
                        $"Select a {loadedPlugin.Name} variant",
                        variants.Select(v => $"{v.Name} - {v.Description}").ToList());

                    if (!int.TryParse(Console.ReadLine(), out int variantChoice) || variantChoice < 0 || variantChoice > variants.Count)
                    {
                        Console.WriteLine("\nInvalid selection.\n");
                        break;
                    }
                    if (variantChoice == 0)
                    {
                        Console.WriteLine("\nCancelled.\n");
                        break;
                    }

                    selectedVariant = variants[variantChoice - 1];
                }

                if (hostMultiplayer)
                {
                    await HostMultiplayerGameAsync(loadedPlugin, selectedVariant);
                }
                else
                {
                    Console.WriteLine(selectedVariant == null
                        ? $"\nStarting '{loadedPlugin.Name}'...\n"
                        : $"\nStarting '{loadedPlugin.Name}' ({selectedVariant.Name})...\n");
                    var gameChannel = new TextGameChannel(gameIo);
                    var gameManager = selectedVariant == null
                        ? loadedPlugin.CreateGameManager(gameChannel)
                        : loadedPlugin.CreateGameManager(gameChannel, selectedVariant);
                    gameManager.StartGame();
                }
                break;
            case 2: // Load Game
                var loadedSettings = settingsService.Load();
                var pluginDirectory = string.IsNullOrWhiteSpace(loadedSettings.PluginDirectory)
                    ? AppContext.BaseDirectory
                    : loadedSettings.PluginDirectory;
                if (!Directory.Exists(pluginDirectory))
                {
                    Console.WriteLine($"\nConfigured plugin directory '{pluginDirectory}' does not exist. Update it via Settings.\n");
                    break;
                }
                var discoveredPlugins = assemblyLoaderService.DiscoverPlugins(pluginDirectory);
                if (discoveredPlugins.Count == 0)
                {
                    Console.WriteLine("\nNo plugins found.\n");
                    break;
                }
                Console.WriteLine();
                for (int i = 0; i < discoveredPlugins.Count; i++)
                {
                    var plugin = discoveredPlugins[i];
                    Console.WriteLine($"{i + 1}) {plugin.Name} (v{plugin.Version}) - {plugin.Description}");
                }
                Console.Write("\nSelect a game to load (0 to cancel): ");
                if (!int.TryParse(Console.ReadLine(), out int gameChoice) || gameChoice < 0 || gameChoice > discoveredPlugins.Count)
                {
                    Console.WriteLine("\nInvalid selection. No game loaded.\n");
                    break;
                }
                if (gameChoice == 0)
                {
                    Console.WriteLine("\nCancelled.\n");
                    break;
                }
                loadedPlugin = discoveredPlugins[gameChoice - 1];
                Console.WriteLine($"\n'{loadedPlugin.Name}' loaded. Select 'Play' to begin.\n");
                break;
            case 3: // Unload Game
                var loadedPluginNames = assemblyLoaderService.GetLoadedPluginNames();
                if (loadedPluginNames.Count == 0)
                {
                    Console.WriteLine("\nNo plugins are currently loaded.\n");
                    break;
                }
                Console.WriteLine("\nLoaded plugins:");
                foreach (var name in loadedPluginNames)
                {
                    Console.WriteLine($"- {name}");
                }
                Console.Write("Enter the name of the plugin to unload: ");
                var contextName = Console.ReadLine() ?? string.Empty;
                Console.WriteLine(assemblyLoaderService.UnloadPluginAssembly(contextName)
                    ? $"\nUnloaded '{contextName}'.\n"
                    : $"\nCould not find a loaded plugin named '{contextName}'.\n");
                break;
            case 4: // Settings
                var currentSettings = settingsService.Load();
                var displayedDirectory = string.IsNullOrWhiteSpace(currentSettings.PluginDirectory)
                    ? $"(not set - defaults to {AppContext.BaseDirectory})"
                    : currentSettings.PluginDirectory;
                Console.WriteLine($"\nCurrent plugin directory: {displayedDirectory}");
                Console.Write("Enter new plugin directory (leave blank to keep current): ");
                var newPluginDirectory = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(newPluginDirectory))
                {
                    if (!Directory.Exists(newPluginDirectory))
                    {
                        Console.WriteLine($"\nWarning: '{newPluginDirectory}' does not currently exist. Saving anyway.");
                    }
                    currentSettings.PluginDirectory = newPluginDirectory;
                }

                Console.WriteLine($"\nMultiplayer enabled: {currentSettings.MultiplayerEnabled}");
                Console.Write("Enable multiplayer? (y/n, leave blank to keep current): ");
                var multiplayerInput = Console.ReadLine()?.Trim();
                if (string.Equals(multiplayerInput, "y", StringComparison.OrdinalIgnoreCase))
                    currentSettings.MultiplayerEnabled = true;
                else if (string.Equals(multiplayerInput, "n", StringComparison.OrdinalIgnoreCase))
                    currentSettings.MultiplayerEnabled = false;

                settingsService.Save(currentSettings);
                settings = currentSettings;
                Console.WriteLine("Settings saved.\n");
                break;
            case 5: // About
                Console.WriteLine("\nCardGames - a plugin-based card game host.\n");
                break;
        }
    }

    async Task HostMultiplayerGameAsync(IPlugin plugin, GameVariant? variant)
    {
        Console.Write("\nEnter your display name (shown to joining players): ");
        var hostDisplayName = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(hostDisplayName))
        {
            Console.WriteLine("\nA display name is required. Cancelled.\n");
            return;
        }

        var totalHumans = PromptForInt(
            $"How many human players total, including you? ({MinTotalHumanPlayers}-{MaxTotalHumanPlayers}): ", MinTotalHumanPlayers, MaxTotalHumanPlayers);
        if (totalHumans == null)
        {
            Console.WriteLine("\nInvalid selection. Cancelled.\n");
            return;
        }

        var aiOpponents = PromptForInt($"How many AI opponents? ({MinAiOpponents}-{MaxAiOpponents}): ", MinAiOpponents, MaxAiOpponents);
        if (aiOpponents == null)
        {
            Console.WriteLine("\nInvalid selection. Cancelled.\n");
            return;
        }

        var joinCode = sessionManager.CreateSession(hostDisplayName, totalHumans.Value - 1, aiOpponents.Value, plugin.Name, plugin.Version);

        await using var gameServerHost = new GameServerHost();
        await gameServerHost.StartAsync(sessionManager, port: 0);

        var localAddresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList
            .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
            .Select(ip => ip.ToString())
            .ToList();

        Console.WriteLine("\nHosting started. Share this with the other player(s):");
        Console.WriteLine($"  Join code: {joinCode}");
        Console.WriteLine($"  Port:      {gameServerHost.Port}");
        if (localAddresses.Count > 0)
            Console.WriteLine($"  Address:   {string.Join(" or ", localAddresses)}");

        var lastRosterCount = 1;
        Console.WriteLine($"\nWaiting for players to join... ({lastRosterCount}/{totalHumans})");
        while (!sessionManager.IsSessionFull)
        {
            await Task.Delay(1000);
            var roster = sessionManager.GetRoster();
            if (roster.Count != lastRosterCount)
            {
                lastRosterCount = roster.Count;
                Console.WriteLine($"Waiting for players to join... ({lastRosterCount}/{totalHumans}) - joined: {string.Join(", ", roster.Select(r => r.PlayerName))}");
            }
        }

        Console.WriteLine("\nAll players joined! Starting the game...\n");
        try
        {
            await sessionManager.StartSessionAsync(plugin, variant);
            Console.WriteLine("\nMultiplayer session ended.\n");
        }
        catch (Exception ex)
        {
            // v1 has no reconnect/resume - a mid-game disconnect aborts the whole session.
            Console.WriteLine($"\nMultiplayer session aborted: {ex.Message}\n");
        }
        finally
        {
            await gameServerHost.StopAsync();
            sessionManager.EndSession();
        }
    }

    async Task JoinMultiplayerGameAsync(IPlugin plugin)
    {
        Console.Write("\nHost address (IP or hostname): ");
        var joinHostAddress = Console.ReadLine()?.Trim();
        Console.Write("Host port: ");
        if (!int.TryParse(Console.ReadLine(), out int joinPort))
        {
            Console.WriteLine("\nInvalid port. Cancelled.\n");
            return;
        }
        Console.Write("Join code: ");
        var enteredJoinCode = Console.ReadLine()?.Trim();
        Console.Write("Your display name: ");
        var joinDisplayName = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(joinHostAddress) || string.IsNullOrWhiteSpace(enteredJoinCode) || string.IsNullOrWhiteSpace(joinDisplayName))
        {
            Console.WriteLine("\nMissing required info. Cancelled.\n");
            return;
        }

        await using var clientConnection = new GameClientConnection(joinHostAddress, joinPort);
        clientConnection.MessageReceived += message => Console.Write(message);

        var promptedSignal = new SemaphoreSlim(0);
        clientConnection.PromptReceived += () => promptedSignal.Release();

        var closedSignal = new TaskCompletionSource();
        clientConnection.ConnectionClosed += _ => closedSignal.TrySetResult();
        clientConnection.SessionAborted += reason =>
        {
            Console.WriteLine($"\nSession aborted: {reason}\n");
            closedSignal.TrySetResult();
        };

        JoinResult joinResult;
        try
        {
            joinResult = await clientConnection.JoinAsync(enteredJoinCode, joinDisplayName, plugin.Name, plugin.Version);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nCould not connect: {ex.Message}\n");
            return;
        }

        if (!joinResult.Success)
        {
            Console.WriteLine($"\nCould not join: {joinResult.ErrorMessage}\n");
            return;
        }

        Console.WriteLine("\nJoined! Waiting for the host to start the game...\n");

        while (true)
        {
            var promptTask = promptedSignal.WaitAsync();
            var completed = await Task.WhenAny(promptTask, closedSignal.Task);
            if (completed == closedSignal.Task)
            {
                Console.WriteLine("\nDisconnected from host. Returning to the menu.\n");
                break;
            }

            var input = Console.ReadLine() ?? string.Empty;
            await clientConnection.SubmitInputAsync(input);
        }
    }

    int? PromptForInt(string prompt, int min, int max)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine(), out var value) && value >= min && value <= max)
            return value;
        return null;
    }
}
catch (Exception e)
{
    Console.WriteLine("Error: {0}", e.Message);
}
finally
{
    Console.WriteLine("Shutting down the host...");
    await host.StopAsync();
    Environment.Exit(0);
}