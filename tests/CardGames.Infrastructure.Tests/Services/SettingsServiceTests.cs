using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;
using CardGames.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CardGames.Infrastructure.Tests.Services;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _TempSettingsPath;
    private readonly ISettingsService _SettingsService;

    public SettingsServiceTests()
    {
        _TempSettingsPath = Path.Combine(Path.GetTempPath(), $"CardGames.Tests.{Guid.NewGuid()}", "settings.json");
        _SettingsService = new SettingsService(new LoggerFactory(), _TempSettingsPath);
    }

    [Fact]
    public void Load_NoFileExists_ReturnsDefaults()
    {
        var settings = _SettingsService.Load();

        Assert.Equal(string.Empty, settings.PluginDirectory);
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsPluginDirectory()
    {
        _SettingsService.Save(new ApplicationSettings { PluginDirectory = "/some/plugins" });

        var reloaded = _SettingsService.Load();

        Assert.Equal("/some/plugins", reloaded.PluginDirectory);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefaultsWithoutThrowing()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_TempSettingsPath)!);
        File.WriteAllText(_TempSettingsPath, "{ not valid json");

        var settings = _SettingsService.Load();

        Assert.Equal(string.Empty, settings.PluginDirectory);
    }

    [Fact]
    public void Save_CreatesMissingDirectory()
    {
        _SettingsService.Save(new ApplicationSettings { PluginDirectory = "/some/plugins" });

        Assert.True(File.Exists(_TempSettingsPath));
    }

    [Fact]
    public void Load_JsonNullLiteral_ReturnsDefaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_TempSettingsPath)!);
        File.WriteAllText(_TempSettingsPath, "null");

        var settings = _SettingsService.Load();

        Assert.Equal(string.Empty, settings.PluginDirectory);
    }

    [Fact]
    public void Save_CalledTwice_OverwritesPreviousValue()
    {
        _SettingsService.Save(new ApplicationSettings { PluginDirectory = "/first" });
        _SettingsService.Save(new ApplicationSettings { PluginDirectory = "/second" });

        var reloaded = _SettingsService.Load();

        Assert.Equal("/second", reloaded.PluginDirectory);
    }

    [Fact]
    public void Save_TargetDirectoryBlockedByFile_DoesNotThrow()
    {
        // Create a file where a directory needs to be, forcing Directory.CreateDirectory to fail.
        var blockingFilePath = Path.Combine(Path.GetTempPath(), $"CardGames.Tests.{Guid.NewGuid()}");
        File.WriteAllText(blockingFilePath, "not a directory");
        try
        {
            var unreachableSettingsPath = Path.Combine(blockingFilePath, "settings.json");
            var service = new SettingsService(new LoggerFactory(), unreachableSettingsPath);

            var exception = Record.Exception(() => service.Save(new ApplicationSettings { PluginDirectory = "/x" }));

            Assert.Null(exception);
        }
        finally
        {
            File.Delete(blockingFilePath);
        }
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_TempSettingsPath);
        if (directory != null && Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
