using Xunit;
using CardGames.Domain;

namespace CardGames.Domain.Tests;

public class DomainMarkerTests
{
    [Fact]
    public void Name_IsExpected()
    {
        Assert.Equal("CardGames.Application", DomainMarker.Name);
    }
}
