using System.Reflection;

namespace UnrealSharp.Loader;

public readonly struct AssemblyInformation(PluginLoadContextWrapper loadContextWrapper, Assembly assembly)
{
    public readonly PluginLoadContextWrapper LoadContextWrapper = loadContextWrapper;
    private readonly WeakReference<Assembly> AssemblyReference = new(assembly);
    
    public bool IsAlive => LoadContextWrapper.IsAlive;
    public bool IsCollectible => LoadContextWrapper.IsCollectible;
    
    public string Path => LoadContextWrapper.AssemblyLoadedPath!;
    public string Name { get; } = assembly.GetName().Name!;
    
    // Be careful when getting a hard reference to the Assembly. It can mess up with Hot Reload.
    public Assembly GetAssembly()
    {
        if (AssemblyReference.TryGetTarget(out var assembly))
        {
            return assembly;
        }
        throw new InvalidOperationException("Assembly is not alive");
    }
}

