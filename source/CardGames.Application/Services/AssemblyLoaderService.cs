using System.Reflection;
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
    /// Verifies if a specific assembly contains a type implementing the given interface.
    /// Uses reflection only loading for security.
    /// </summary>
    /// <param name="assemblyPath"></param>
    /// <param name="interfaceType"></param>
    /// <returns></returns>
    public bool VerifyAssemblyInterfaces(string assemblyPath, Type interfaceType)
    {
        try
        {
            var trustedPlatformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator);
            var resolver = new PathAssemblyResolver(trustedPlatformAssemblies.Append(assemblyPath));
            using var loadContext = new MetadataLoadContext(resolver);

            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            return assembly.GetExportedTypes()
                .Any(type => type.GetInterfaces().Any(i => i.FullName == interfaceType.FullName));
        }
        catch (Exception e)
        {
            _Logger.LogError($"Assembly Verify Exception: {e.Message}");
        }
        return false;
    }

    /// <summary>
    /// Scans a directory for plugin assemblies ('*.plugin.dll'), verifies and loads each one
    /// into its own collectible AssemblyLoadContext, and returns the discovered IPlugin instances.
    /// </summary>
    /// <param name="directoryPath"></param>
    /// <returns></returns>
    public IReadOnlyList<IPlugin> DiscoverPlugins(string directoryPath)
    {
        var discoveredPlugins = new List<IPlugin>();

        foreach (var assemblyPath in Directory.GetFiles(directoryPath, "*.plugin.dll"))
        {
            if (!VerifyAssemblyInterfaces(assemblyPath, typeof(IPlugin)))
            {
                _Logger.LogWarning($"Assembly '{assemblyPath}' does not implement {nameof(IPlugin)}, skipping.");
                continue;
            }

            if (!LoadPluginAssembly(assemblyPath))
                continue;

            var contextName = Path.GetFileNameWithoutExtension(assemblyPath);
            var context = _AssemblyLoadContexts.FirstOrDefault(x => string.Compare(x.Name, contextName, true) == 0);
            var assembly = context?.Assemblies.FirstOrDefault();
            if (assembly == null)
                continue;

            var pluginTypes = assembly.GetExportedTypes()
                .Where(type => typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract);

            foreach (var pluginType in pluginTypes)
            {
                if (Activator.CreateInstance(pluginType) is IPlugin plugin)
                    discoveredPlugins.Add(plugin);
            }
        }

        return discoveredPlugins;
    }

    /// <summary>
    /// Returns the context names of all currently loaded plugin assemblies.
    /// </summary>
    public IReadOnlyList<string> GetLoadedPluginNames()
    {
        return _AssemblyLoadContexts.Select(x => x.Name!).ToList();
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
        var context = _AssemblyLoadContexts.FirstOrDefault(x => string.Compare(x.Name, contextName, true) == 0);
        // Null Check
        if (context == null)
            return false;
        // Unload
        context.Unload();
        _AssemblyLoadContexts.Remove(context);
        return true;
    }
}
