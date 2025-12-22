using CardGames.Application.Tests.Fixtures;
using Xunit;

namespace CardGames.Application.Services;

public class AssemblyServiceTests : IClassFixture<AssemblyServiceFixture>
{
    private readonly AssemblyServiceFixture _Fixture;

    public AssemblyServiceTests(AssemblyServiceFixture fixture)
    {
        _Fixture = fixture;
    }

    [Fact]
    public void LoadAssembly()
    {
        _Fixture.LoaderService.LoadPluginAssembly(@"C:\Users\mmiha\source\repos\CardGames\Deploy\Debug\netcoreapp3.0\M2.CardGames.War.dll");
    }

    [Fact]
    public void UnloadAssembly()
    {
        Assert.True(_Fixture.LoaderService.LoadPluginAssembly(@"C:\Users\mmiha\source\repos\CardGames\Deploy\Debug\netcoreapp3.0\M2.CardGames.War.dll"));
        Assert.True(_Fixture.LoaderService.UnloadPluginAssembly("M2.CardGames.War"));
    }
}