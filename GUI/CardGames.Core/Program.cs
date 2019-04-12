using M2.CardGames.Common;
using M2.CardGames.Common.Interfaces;
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
        private readonly ILogger<Program> _Logger;
        static Dictionary<int, string> _CommandDictionary = LoadCommandDictionary();

        static void Main(string[] args)
        {
            // INIT
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Trace);
                })
                .AddSingleton<IAssemblyLoaderService, AssemblyLoaderService>()
                .BuildServiceProvider();

            // Log Creation
            var _Logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();

            // Main Code


            /*
            int selection = -1;
            while (!_CommandDictionary.Keys.Contains(selection))
            {
                DisplayMenu();
                if (!int.TryParse(Console.ReadLine(), out int result))
                {
                    Console.WriteLine("\nInvalid Entry! Try Again.\n");
                    continue;
                }
                _Logger.LogDebug($"Parsed entry: {result}");
                selection = result;
            }

            DeckOfCards deckOfCards = new DeckOfCards();
            deckOfCards.Shuffle();
            var card = deckOfCards.Draw();

            */
            var service = serviceProvider.GetRequiredService<IAssemblyLoaderService>();
            service.VerifyAssemblyInterfaces(@"C:\Users\mmiha\source\repos\CardGames\Deploy\Debug\netstandard2.0\M2.CardGames.War.dll", typeof(ICardGameInterface));


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
