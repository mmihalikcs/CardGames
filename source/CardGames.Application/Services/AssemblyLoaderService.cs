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

            // TRUSTED_PLATFORM_ASSEMBLIES alone is enough to resolve a plugin's dependencies (e.g.
            // CardGames.Domain) under a normal dotnet-hosted apphost, since it lists every dependency
            // of the running app. Hosts with their own runtime bootstrap - e.g. Godot's Mono build -
            // populate it with only their own platform assemblies, so every DLL sitting next to the
            // plugin (where the build/copy step already places its dependencies) is trusted too, kept
            // distinct by simple name so PathAssemblyResolver never sees two paths for the same name.
            var knownNames = new HashSet<string>(
                trustedPlatformAssemblies.Select(Path.GetFileName)!,
                StringComparer.OrdinalIgnoreCase)
            {
                Path.GetFileName(assemblyPath)!
            };

            var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
            var neighboringAssemblies = string.IsNullOrEmpty(assemblyDirectory) || !Directory.Exists(assemblyDirectory)
                ? Enumerable.Empty<string>()
                : Directory.GetFiles(assemblyDirectory, "*.dll").Where(path => knownNames.Add(Path.GetFileName(path)!));

            var resolver = new PathAssemblyResolver(trustedPlatformAssemblies.Concat(neighboringAssemblies).Append(assemblyPath));
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

            // Under a normal dotnet-hosted apphost, a plugin's dependencies (e.g. CardGames.Domain)
            // resolve automatically because they're part of the host's own TRUSTED_PLATFORM_ASSEMBLIES
            // and CoreCLR's default binder satisfies any ALC's load request from that process-wide
            // list first. Hosts with their own runtime bootstrap (e.g. Godot's Mono build) don't put
            // the app's own dependencies there, so this ALC needs its own fallback: look for a
            // same-named DLL next to the plugin itself, where the build/copy step already places it.
            var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
            context.Resolving += (loadContext, name) => ResolveNeighboringAssembly(loadContext, name, assemblyDirectory);

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

    private static Assembly? ResolveNeighboringAssembly(AssemblyLoadContext context, AssemblyName assemblyName, string? assemblyDirectory)
    {
        // Prefer an assembly already loaded anywhere in the process (e.g. CardGames.Domain, loaded
        // into the Default context as the host's own dependency) over loading a second copy into
        // this plugin's context. Two separately-loaded copies of the same DLL produce two distinct,
        // incompatible CLR types even with identical bytes, so a plugin's IPlugin implementation
        // would silently fail typeof(IPlugin).IsAssignableFrom(...) against the host's IPlugin type
        // if it bound to its own private copy instead of the host's.
        var alreadyLoaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
        if (alreadyLoaded is not null)
            return alreadyLoaded;

        if (string.IsNullOrEmpty(assemblyDirectory) || assemblyName.Name is null)
            return null;

        var candidatePath = Path.Combine(assemblyDirectory, assemblyName.Name + ".dll");
        return File.Exists(candidatePath) ? context.LoadFromAssemblyPath(candidatePath) : null;
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
