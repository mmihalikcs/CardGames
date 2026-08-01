using CardGames.Domain.Models;

namespace CardGames.Domain.Interfaces;

public interface ISettingsService
{
    ApplicationSettings Load();
    void Save(ApplicationSettings settings);
}
