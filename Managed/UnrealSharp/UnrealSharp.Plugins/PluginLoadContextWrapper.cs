using System.Reflection;
using System.Runtime.Loader;

namespace UnrealSharp.Plugins;

public sealed class PluginLoadContextWrapper
{
    private PluginLoadContext? _pluginLoadContext;
    private readonly WeakReference _weakReference;

    private PluginLoadContextWrapper(PluginLoadContext pluginLoadContext, WeakReference weakReference)
    {
        _pluginLoadContext = pluginLoadContext;
        _weakReference = weakReference;
    }

    public string? AssemblyLoadedPath => _pluginLoadContext?.AssemblyLoadedPath;
    public bool IsCollectible => _pluginLoadContext?.IsCollectible ?? true;
    public bool IsAlive => _weakReference.IsAlive;

    public static AssemblyInformation CreateAndLoadFromAssemblyName(AssemblyName assemblyName, 
        string pluginPath, 
        ICollection<string> sharedAssemblies, 
        AssemblyLoadContext mainLoadContext, 
        bool isCollectible, 
        IntPtr unmanagedCallbacks, 
        IntPtr exportFunctionsPtr)
    {
        var context = new PluginLoadContext(pluginPath, sharedAssemblies, mainLoadContext, isCollectible);
        var reference = new WeakReference(context, trackResurrection: true);
        var wrapper = new PluginLoadContextWrapper(context, reference);
        var assembly = context.LoadFromAssemblyName(assemblyName);
        return new AssemblyInformation(assembly, wrapper, PluginsHelper.FindModule(assembly, [unmanagedCallbacks, exportFunctionsPtr]));
    }
        
    internal void Unload()
    {
        _pluginLoadContext!.Unload();
        _pluginLoadContext = null;
    }
}