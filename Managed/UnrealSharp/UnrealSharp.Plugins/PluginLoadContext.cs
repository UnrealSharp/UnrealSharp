using System.Reflection;
using System.Runtime.Loader;

namespace UnrealSharp.Plugins;

public class PluginLoadContext(AssemblyDependencyResolver resolver, bool isCollectible) : AssemblyLoadContext(isCollectible)
{
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName.Name))
        {
            return default;
        }
        
        foreach (Assembly sharedAssembly in PluginLoader.SharedAssemblies)
        {
            if (sharedAssembly.GetName().Name == assemblyName.Name)
            {
                return Main.MainLoadContext.LoadFromAssemblyName(assemblyName);
            }
        }
        
        foreach (var loadedPlugin in PluginLoader.LoadedPlugins)
        {
            if (!loadedPlugin.IsAssemblyAlive || loadedPlugin.WeakRefAssembly?.Target is not Assembly assembly)
            {
                continue;
            }

            if (assembly.GetName() == assemblyName)
            {
                return assembly;
            }
        }

        string? assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);

        if (string.IsNullOrEmpty(assemblyPath))
        {
            return null;
        }

        using FileStream assemblyFile = File.Open(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");

        if (!File.Exists(pdbPath))
        {
            return LoadFromStream(assemblyFile);
        }

        using var pdbFile = File.Open(pdbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return LoadFromStream(assemblyFile, pdbFile);
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : nint.Zero;
    }
}
