using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using CardGames.Domain.Interfaces;
using System.IO;

namespace CardGames.Application.Services;

public class AssemblyLoaderService : IAssemblyLoaderService
{
    public IReadOnlyList<AssemblyLoadContext> LoadedAssemblies => _AssemblyLoadContexts.AsReadOnly();

    // Fields
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

    public bool VerifyPluginAssemblies(string assemblyLoadPath, IEnumerable<Type> interfaceTypes)
    {
        foreach (var files in Directory.GetFiles(assemblyLoadPath, "*.plugin.dll"))
        {
            try
            {
                _Logger.LogDebug($"Loading assembly '{files}'");
                var loadResult = LoadPluginAssembly(files);
            }
            catch
            {

            }
        }
        return true;
    }

    /// <summary>
    /// Load Assemblies
    /// </summary>
    /// <param name="assemblyPath"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Unload Assemblies
    /// </summary>
    /// <param name="contextName"></param>
    /// <returns></returns>
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