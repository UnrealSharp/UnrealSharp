using System.Reflection;
using System.Runtime.Loader;

namespace UnrealSharp.Plugins;

public class PluginLoadContext(AssemblyDependencyResolver resolver, bool isCollectible) : AssemblyLoadContext(isCollectible)
{
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName.Name)) return default;

        if (PluginLoader.SharedAssemblies.Any(a => a.FullName == assemblyName.FullName))
        {
            return GetLoadContext(Assembly.GetExecutingAssembly())!.LoadFromAssemblyName(assemblyName);
        }

        //check if assembly already loaded in another plugin
        var loadedAssembly = PluginLoader.LoadedPlugins
            .Where(p => p.IsAssemblyAlive && p.WeakRefAssembly!.Target is Assembly)
            .Select(p => (Assembly)p.WeakRefAssembly!.Target!)
            .FirstOrDefault(a => a.FullName == assemblyName.FullName);

        if (loadedAssembly != null)
        {
            return loadedAssembly;
        }

        string? assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);

        if (string.IsNullOrEmpty(assemblyPath))
        {
            return default;
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
