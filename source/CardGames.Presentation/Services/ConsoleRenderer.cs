using CardGames.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CardGames.Presentation.Services;

public class ConsoleRenderer
{
    // Properties
    public IReadOnlyDictionary<int, string> Commands => _BaseCommandDictionary.AsReadOnly();

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
    /// Main Function to render the commands menu
    /// </summary>
    public void DisplayMenu()
    {
        // Run Base Query
        var query = _BaseCommandDictionary.Where(x => x.Key != 0).OrderBy(f => f.Key).ToList();
        // Process the list
        foreach (var command in query)
        {
            Console.WriteLine($"{command.Key}) {command.Value}");
        }
        // Attach Exit to the bottom
        Console.WriteLine($"0) Exit");
        // Print selection
        Console.Write("Enter a selection: ");
    }

    // Private Members
    private Dictionary<int, string> LoadCommandDictionary()
    {
        return new Dictionary<int, string>()
        {
            { 1, "Play" },
            { 2, "Load Game" },
            { 3, "Unload Game" },
            { 4, "About" },
            { 0, "Exit" },
        };
    }
}
