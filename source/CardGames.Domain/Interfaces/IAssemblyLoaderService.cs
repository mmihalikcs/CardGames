namespace CardGames.Domain.Interfaces;

public interface IAssemblyLoaderService
{
    bool VerifyAssemblyInterfaces(string assemblyPath, Type interfaceType);
    bool LoadPluginAssembly(string assemblyPath);
    bool UnloadPluginAssembly(string contextName);
}