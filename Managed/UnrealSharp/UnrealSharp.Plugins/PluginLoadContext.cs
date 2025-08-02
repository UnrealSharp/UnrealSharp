using System.Reflection;
using System.Runtime.Loader;

namespace UnrealSharp.Plugins;

public class PluginLoadContext : AssemblyLoadContext
{
    public PluginLoadContext(string assemblyName, AssemblyDependencyResolver resolver, bool isCollectible) : base(assemblyName, isCollectible)
    {
        _resolver = resolver;
    }

    private readonly AssemblyDependencyResolver _resolver;

    // Cache of loaded assemblies by their name
    private static readonly Dictionary<string, WeakReference<Assembly>> _loadedAssemblies = new();

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName.Name))
        {
            return null;
        }

        // Check if the assembly is already loaded in our cache
        if (_loadedAssemblies.TryGetValue(assemblyName.Name, out var weakRef) &&
            weakRef.TryGetTarget(out var cachedAssembly))
        {
            return cachedAssembly;
        }

        foreach (Assembly sharedAssembly in PluginLoader.SharedAssemblies)
        {
            if (sharedAssembly.GetName().Name == assemblyName.Name)
            {
                var loadedSharedAssembly = Main.MainLoadContext.LoadFromAssemblyName(assemblyName);
                // Cache the loaded assembly
                _loadedAssemblies[assemblyName.Name] = new WeakReference<Assembly>(loadedSharedAssembly);
                return loadedSharedAssembly;
            }
        }

        foreach (var loadedPlugin in PluginLoader.LoadedPlugins)
        {
            if (!loadedPlugin.IsAssemblyAlive || loadedPlugin.WeakRefAssembly?.Target is not Assembly assembly)
            {
                continue;
            }

            string loadedAssemblyName = assembly.GetName().Name;
            if (loadedAssemblyName == assemblyName.Name)
            {
                // Cache the assembly
                _loadedAssemblies[assemblyName.Name] = new WeakReference<Assembly>(assembly);
                return assembly;
            }
        }

        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

        if (string.IsNullOrEmpty(assemblyPath))
        {
            return null;
        }

        using FileStream assemblyFile = File.Open(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");

        Assembly? loadedAssembly;
        if (!File.Exists(pdbPath))
        {
            loadedAssembly = LoadFromStream(assemblyFile);
        }
        else
        {
            using var pdbFile = File.Open(pdbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            loadedAssembly = LoadFromStream(assemblyFile, pdbFile);
        }

        // Cache the loaded assembly
        _loadedAssemblies[assemblyName.Name] = new WeakReference<Assembly>(loadedAssembly);

        return loadedAssembly;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : nint.Zero;
    }
}
