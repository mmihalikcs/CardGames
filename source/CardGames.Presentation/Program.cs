using CardGames.Application.Services;
using CardGames.Domain.Interfaces;
using CardGames.Infrastructure.Services;
using CardGames.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;

// Generic Host Creation
using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        //services.AddLogging(configure => configure.AddConsole());

        // DI
        services.AddSingleton<IAssemblyLoaderService, AssemblyLoaderService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ConsoleRenderer>();
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
    var consoleRenderer = host.Services.GetRequiredService<ConsoleRenderer>();

    // Main Loop
    int selection = -1;
    while (selection != 0)
    {
        consoleRenderer.DisplayMenu();
        if (!int.TryParse(Console.ReadLine(), out int result) || !consoleRenderer.Commands.ContainsKey(result))
        {
            Console.WriteLine("\nInvalid Entry! Try Again.\n");
            continue;
        }
        logger.LogDebug("Parsed entry: {Selection}", result);
        selection = result;

        switch (selection)
        {
            case 1: // Play
                Console.WriteLine("\nPlay is not implemented yet.\n");
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
                foreach (var plugin in discoveredPlugins)
                {
                    Console.WriteLine($"{plugin.Name} (v{plugin.Version}) - {plugin.Description}");
                }
                Console.WriteLine();
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

                if (string.IsNullOrWhiteSpace(newPluginDirectory))
                {
                    Console.WriteLine("\nNo changes made.\n");
                    break;
                }

                if (!Directory.Exists(newPluginDirectory))
                {
                    Console.WriteLine($"\nWarning: '{newPluginDirectory}' does not currently exist. Saving anyway.");
                }

                currentSettings.PluginDirectory = newPluginDirectory;
                settingsService.Save(currentSettings);
                Console.WriteLine("Settings saved.\n");
                break;
            case 5: // About
                Console.WriteLine("\nCardGames - a plugin-based card game host.\n");
                break;
        }
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