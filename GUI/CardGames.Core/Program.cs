using M2.CardGames.Common;
using M2.CardGames.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace M2.CardGames.Core
{
    class Program
    {
        static Dictionary<int, string> _CommandDictionary = LoadCommandDictionary();

        static void Main(string[] args)
        {
            // INIT
            var serviceCollection = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IAssemblyLoaderService, AssemblyLoaderService>()
                .BuildServiceProvider();
                                
            // Main Code


            int selection = -1;
            while (!_CommandDictionary.Keys.Contains(selection))
            {
                DisplayMenu();
                if (!int.TryParse(Console.ReadLine(), out int result))
                {
                    Console.WriteLine("\nInvalid Entry! Try Again.\n");
                    continue;
                }
                selection = result;
            }

            DeckOfCards deckOfCards = new DeckOfCards();
            deckOfCards.Shuffle();
            var card = deckOfCards.Draw();


            Console.Write("\nPress any key to exit...");
            Console.ReadKey();        
        }

        static void DisplayMenu()
        {
            foreach (var command in _CommandDictionary.OrderBy(f => f.Key))
                Console.WriteLine($"{command.Key}) {command.Value}");
            Console.Write("Enter a selection: ");
        }

        static Dictionary<int, string> LoadCommandDictionary()
        {
            return new Dictionary<int, string>()
            {
                { 1, "Play" },
                { 2, "Load Game" },
                { 3, "Unload Game" },
                { 4, "About" },
                { 9, "Exit" },
            };
        }
    }
}
