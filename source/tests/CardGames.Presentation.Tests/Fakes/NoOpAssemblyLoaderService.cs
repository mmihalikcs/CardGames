using CardGames.Domain.Interfaces;

namespace CardGames.Presentation.Tests.Fakes;

internal sealed class NoOpAssemblyLoaderService : IAssemblyLoaderService
{
    public bool VerifyAssemblyInterfaces(string assemblyPath, Type interfaceType) => false;
    public bool LoadPluginAssembly(string assemblyPath) => false;
    public bool UnloadPluginAssembly(string contextName) => false;
    public IReadOnlyList<IPlugin> DiscoverPlugins(string directoryPath) => Array.Empty<IPlugin>();
    public IReadOnlyList<string> GetLoadedPluginNames() => Array.Empty<string>();
}
