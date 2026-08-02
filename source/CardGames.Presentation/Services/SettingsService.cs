using System.Text.Json;
using CardGames.Domain.Interfaces;
using CardGames.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CardGames.Presentation.Services;

public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    // Fields
    private readonly ILogger<SettingsService> _Logger;
    private readonly string _SettingsFilePath;

    public SettingsService(ILoggerFactory loggingFactory)
        : this(loggingFactory, GetDefaultSettingsFilePath())
    {
    }

    internal SettingsService(ILoggerFactory loggingFactory, string settingsFilePath)
    {
        _Logger = loggingFactory.CreateLogger<SettingsService>();
        _SettingsFilePath = settingsFilePath;
    }

    /// <summary>
    /// Loads settings from disk. Returns defaults if the file is missing or unreadable.
    /// </summary>
    public ApplicationSettings Load()
    {
        try
        {
            if (!File.Exists(_SettingsFilePath))
                return new ApplicationSettings();

            var json = File.ReadAllText(_SettingsFilePath);
            return JsonSerializer.Deserialize<ApplicationSettings>(json) ?? new ApplicationSettings();
        }
        catch (Exception e)
        {
            _Logger.LogWarning(e, "Settings Load Exception");
        }
        return new ApplicationSettings();
    }

    /// <summary>
    /// Serializes and saves settings to disk, creating the target directory if needed.
    /// </summary>
    public void Save(ApplicationSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_SettingsFilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(settings, SerializerOptions);
            File.WriteAllText(_SettingsFilePath, json);
        }
        catch (Exception e)
        {
            _Logger.LogError(e, "Settings Save Exception");
        }
    }

    private static string GetDefaultSettingsFilePath()
    {
        var configRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(configRoot, "CardGames", "settings.json");
    }
}
