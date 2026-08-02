using CardGames.Application.Services;
using CardGames.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CardGames.Application.Services;

public class AssemblyServiceTests
{
    private static readonly string WARPluginPath = Path.Combine(AppContext.BaseDirectory, "CardGames.WAR.plugin.dll");
    private static readonly string DomainAssemblyPath = Path.Combine(AppContext.BaseDirectory, "CardGames.Domain.dll");

    // A fresh service per test - AssemblyLoadContext state (loaded/unloaded plugins) must not leak across tests.
    private readonly IAssemblyLoaderService _LoaderService;

    public AssemblyServiceTests()
    {
        _LoaderService = new AssemblyLoaderService(new LoggerFactory());
    }

    [Fact]
    public void LoadAssembly()
    {
        Assert.True(_LoaderService.LoadPluginAssembly(WARPluginPath));
    }

    [Fact]
    public void UnloadAssembly()
    {
        Assert.True(_LoaderService.LoadPluginAssembly(WARPluginPath));
        Assert.True(_LoaderService.UnloadPluginAssembly("CardGames.WAR.plugin"));
    }

    [Fact]
    public void UnloadAssembly_UnknownContext_ReturnsFalse()
    {
        Assert.False(_LoaderService.UnloadPluginAssembly("NotALoadedPlugin"));
    }

    [Fact]
    public void VerifyAssemblyInterfaces_PluginAssembly_ReturnsTrue()
    {
        Assert.True(_LoaderService.VerifyAssemblyInterfaces(WARPluginPath, typeof(IPlugin)));
    }

    [Fact]
    public void VerifyAssemblyInterfaces_NonPluginAssembly_ReturnsFalse()
    {
        Assert.False(_LoaderService.VerifyAssemblyInterfaces(DomainAssemblyPath, typeof(IPlugin)));
    }

    [Fact]
    public void DiscoverPlugins_FindsWarAndGoFish()
    {
        var plugins = _LoaderService.DiscoverPlugins(AppContext.BaseDirectory);

        Assert.Contains(plugins, p => p.Name == "WAR!");
        Assert.Contains(plugins, p => p.Name == "Go Fish");
    }

    [Fact]
    public void DiscoverPlugins_DirectoryDoesNotExist_Throws()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), $"CardGames.Tests.Missing.{Guid.NewGuid()}");

        Assert.Throws<DirectoryNotFoundException>(() => _LoaderService.DiscoverPlugins(missingDirectory));
    }

    [Fact]
    public void DiscoverPlugins_NonPluginAssemblyNamedAsPlugin_IsSkipped()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"CardGames.Tests.{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            File.Copy(DomainAssemblyPath, Path.Combine(tempDirectory, "CardGames.Domain.plugin.dll"));

            var plugins = _LoaderService.DiscoverPlugins(tempDirectory);

            Assert.Empty(plugins);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void LoadPluginAssembly_InvalidPath_ReturnsFalse()
    {
        var missingAssemblyPath = Path.Combine(AppContext.BaseDirectory, "DoesNotExist.plugin.dll");

        Assert.False(_LoaderService.LoadPluginAssembly(missingAssemblyPath));
    }

    [Fact]
    public void GetLoadedPluginNames_AfterLoad_ContainsLoadedContext()
    {
        _LoaderService.LoadPluginAssembly(WARPluginPath);

        Assert.Contains("CardGames.WAR.plugin", _LoaderService.GetLoadedPluginNames());
    }

    [Fact]
    public void LoadPluginAssembly_SameAssemblyLoadedTwice_CreatesSeparateContexts()
    {
        // Documents current behavior: the service does not de-duplicate by context name,
        // so loading the same plugin twice produces two entries with the same name.
        Assert.True(_LoaderService.LoadPluginAssembly(WARPluginPath));
        Assert.True(_LoaderService.LoadPluginAssembly(WARPluginPath));

        var loadedNames = _LoaderService.GetLoadedPluginNames();

        Assert.Equal(2, loadedNames.Count(name => name == "CardGames.WAR.plugin"));
    }
}
