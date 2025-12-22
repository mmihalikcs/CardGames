using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using CardGames.Domain.Interfaces;

namespace CardGames.Application.Services;

public class AssemblyLoaderService : IAssemblyLoaderService
{
    private readonly List<AssemblyLoadContext> _AssemblyLoadContexts;
    private readonly ILogger<AssemblyLoaderService> _Logger;

    public AssemblyLoaderService(ILoggerFactory loggingFactory)
    {
        _Logger = loggingFactory.CreateLogger<AssemblyLoaderService>();
        _AssemblyLoadContexts = new List<AssemblyLoadContext>(10);
    }

    /// <summary>
    /// Verifies if a specific assembly contains a type
    /// Uses reflection only loading for security.
    /// </summary>
    /// <param name="assemblyPath"></param>
    /// <param name="interfaceType"></param>
    /// <returns></returns>
    public bool VerifyAssemblyInterfaces(string assemblyPath, Type interfaceType)
    {
        return false;
    }

    // Load Assembly
    public bool LoadPluginAssembly(string assemblyPath)
    {
        try
        {
            AssemblyLoadContext context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(assemblyPath), true);
            context.LoadFromAssemblyPath(assemblyPath);
            _AssemblyLoadContexts.Add(context);
            return true;
        }
        catch (Exception e)
        {
            _Logger.LogError($"Assembly Load Exception: {e.Message}");
        }
        return false;
    }

    public bool UnloadPluginAssembly(string contextName)
    {
        var context = _AssemblyLoadContexts.Where(x => string.Compare(x.Name, contextName, true) == 0).First();
        // Null Check
        if (context == null)
            return false;
        // Unload
        context.Unload();
        _AssemblyLoadContexts.Remove(context);
        return true;
    }
}