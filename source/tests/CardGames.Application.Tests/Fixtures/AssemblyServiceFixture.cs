using CardGames.Application.Services;
using CardGames.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CardGames.Application.Tests.Fixtures;

public class AssemblyServiceFixture
{
    public ILoggerFactory LoggerFactory { get; init; }
    public IAssemblyLoaderService LoaderService { get; init; }

    public AssemblyServiceFixture()
    {
        LoggerFactory = new LoggerFactory();
        LoaderService = new AssemblyLoaderService(LoggerFactory);
    }
}