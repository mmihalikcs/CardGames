using CardGames.Application.Services;
using CardGames.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Generic Host Creation
using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddLogging(configure => configure.AddConsole());

        // DI
        services.AddSingleton<IAssemblyLoaderService, AssemblyLoaderService>();
    })
    .Build();


// Run the host
await host.RunAsync();