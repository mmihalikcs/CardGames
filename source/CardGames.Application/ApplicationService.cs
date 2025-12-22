using CardGames.Domain;

namespace CardGames.Application;

public class ApplicationService
{
    public string GetName() => DomainMarker.Name + ".Application";
}
