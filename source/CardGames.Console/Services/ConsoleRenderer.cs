using CardGames.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CardGames.Console.Services;

public class ConsoleRenderer
{
    // Fields
    private readonly Dictionary<int, string> _BaseCommandDictionary;
    private readonly ILogger<ConsoleRenderer> _Logger;
    private readonly IAssemblyLoaderService _AssemblyLoaderService;

    public ConsoleRenderer(ILogger<ConsoleRenderer> logger, IAssemblyLoaderService assemblyLoaderService)
    {
        _Logger = logger;
        _AssemblyLoaderService = assemblyLoaderService;
        _BaseCommandDictionary = LoadCommandDictionary();
    }

    /// <summary>
    /// The current command set.
    /// </summary>
    public IReadOnlyDictionary<int, string> GetCommands()
    {
        return _BaseCommandDictionary.AsReadOnly();
    }

    /// <summary>
    /// Main Function to render the commands menu
    /// </summary>
    public void DisplayMenu()
    {
        // Run Base Query
        var query = GetCommands().Where(x => x.Key != 0).OrderBy(f => f.Key).ToList();
        // Process the list
        foreach (var command in query)
        {
            System.Console.WriteLine($"{command.Key}) {command.Value}");
        }
        // Attach Exit to the bottom
        System.Console.WriteLine($"0) Exit");
        // Print selection
        System.Console.Write("Enter a selection: ");
    }

    /// <summary>
    /// Renders a generic numbered submenu (e.g. a plugin's selectable game variants) with a
    /// "0) Cancel" option. Write-only, like DisplayMenu() - callers own reading the selection.
    /// </summary>
    public void DisplaySubmenu(string title, IReadOnlyList<string> options)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"{title}:");
        for (int i = 0; i < options.Count; i++)
        {
            System.Console.WriteLine($"{i + 1}) {options[i]}");
        }
        System.Console.WriteLine("0) Cancel");
        System.Console.Write("Enter a selection: ");
    }

    // Private Members
    private Dictionary<int, string> LoadCommandDictionary()
    {
        return new Dictionary<int, string>()
        {
            { 1, "Play" },
            { 2, "Load Game" },
            { 3, "Unload Game" },
            { 4, "Settings" },
            { 5, "About" },
            { 0, "Exit" },
        };
    }
}
