using System.Reflection;

namespace UnrealSharp.Plugins;

internal class PluginLoadContextWrapper
{
    private PluginLoadContext? _pluginLoadContext;

    private PluginLoadContextWrapper(PluginLoadContext pluginLoadContext, Assembly assembly)
    {
        _pluginLoadContext = pluginLoadContext;
        AssemblyFullName = assembly.FullName;
        Assembly = new WeakReference<Assembly>(assembly);
    }

    public string? AssemblyLoadedPath => _pluginLoadContext?.AssemblyLoadedPath;
    public bool IsCollectible => _pluginLoadContext?.IsCollectible ?? true;
    public bool IsAlive => _pluginLoadContext != null;
    public string? AssemblyFullName { get; private set; }
        
    // Be careful using this. Any hard reference at the wrong time will prevent the plugin from being unloaded.
    // Thus breaking hot reloading.
    public WeakReference<Assembly> Assembly { get; }

    public static (Assembly, PluginLoadContextWrapper) CreateAndLoadFromAssemblyName(AssemblyName assemblyName, string pluginPath, ICollection<string> sharedAssemblies, bool isCollectible)
    {
        PluginLoadContext context = new PluginLoadContext(pluginPath, sharedAssemblies, isCollectible);
        Assembly assembly = context.LoadFromAssemblyName(assemblyName);
        PluginLoadContextWrapper wrapper = new PluginLoadContextWrapper(context, assembly);
        return (assembly, wrapper);
    }
        
    internal void Unload()
    {
        _pluginLoadContext?.Unload();
        _pluginLoadContext = null;
    }
}