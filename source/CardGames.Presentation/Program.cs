using CardGames.Application.Services;
using CardGames.Domain.Interfaces;
using CardGames.Presentation.Extensions;
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
    var consoleRenderer = host.Services.GetRequiredService<ConsoleRenderer>();

    // Load up all plugins
    assemblyLoaderService.VerifyAssemblyInterfaces(@"C:\Users\mmiha\source\repos\CardGames\Deploy\Debug\netstandard2.0\M2.CardGames.War.dll", typeof(IPlugin));

    // Main Loop
    int selection = -1;
    while (!consoleRenderer.Commands.Keys.Contains(selection))
    {
        consoleRenderer.DisplayMenu();
        if (!int.TryParse(Console.ReadLine(), out int result))
        {
            Console.WriteLine("\nInvalid Entry! Try Again.\n");
            continue;
        }
        logger.LogDebug($"Parsed entry: {result}");
        selection = result;
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