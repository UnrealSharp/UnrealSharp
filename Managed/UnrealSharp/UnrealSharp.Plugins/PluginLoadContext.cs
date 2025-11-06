using System.Reflection;
using System.Runtime.Loader;
using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Plugins;

public class PluginLoadContext : AssemblyLoadContext
{
    public PluginLoadContext(string assemblyName, AssemblyDependencyResolver resolver, bool isCollectible) : base(assemblyName, isCollectible)
    {
        _resolver = resolver;
    }

    private readonly AssemblyDependencyResolver _resolver;
    private static readonly Dictionary<string, WeakReference<Assembly>> LoadedAssemblies = new();
    
    static PluginLoadContext()
    {
        AddAssembly(typeof(PluginLoader).Assembly);
        AddAssembly(typeof(NativeBinds).Assembly);
        AddAssembly(typeof(UnrealSharpObject).Assembly);
        AddAssembly(typeof(UnrealSharpModule).Assembly);
    }
    
    public static void AddAssembly(Assembly assembly)
    {
        LoadedAssemblies[assembly.GetName().Name!] = new WeakReference<Assembly>(assembly);
    }
    
    public static void RemoveAssemblyFromCache(string assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName))
        {
            return;
        }
        
        LoadedAssemblies.Remove(assemblyName);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName.Name))
        {
            return null;
        }
        
        if (LoadedAssemblies.TryGetValue(assemblyName.Name, out WeakReference<Assembly>? weakRef) && weakRef.TryGetTarget(out Assembly? cachedAssembly))
        {
            return cachedAssembly;
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
            loadedAssembly = LoadFromAssemblyPath(assemblyPath);
        }
        else
        {
            using var pdbFile = File.Open(pdbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            loadedAssembly = LoadFromStream(assemblyFile, pdbFile);
        }
        
        LoadedAssemblies[assemblyName.Name] = new WeakReference<Assembly>(loadedAssembly);
        return loadedAssembly;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : nint.Zero;
    }
}
