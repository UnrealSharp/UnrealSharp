using System.Reflection;
using UnrealSharp.Engine.Core.Modules;

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
    public IModuleInterface? ModuleInterface;
        
    // Be careful using this. Any hard reference at the wrong time will prevent the plugin from being unloaded.
    // Thus breaking hot reloading.
    public WeakReference<Assembly> Assembly { get; }

    public static (Assembly, PluginLoadContextWrapper) CreateAndLoadFromAssemblyName(AssemblyName assemblyName, string pluginPath, ICollection<string> sharedAssemblies, bool isCollectible)
    {
        PluginLoadContext context = new PluginLoadContext(pluginPath, sharedAssemblies, isCollectible);
        Assembly assembly = context.LoadFromAssemblyName(assemblyName);
        PluginLoadContextWrapper wrapper = new PluginLoadContextWrapper(context, assembly);
        
        if (!wrapper.IsAlive)
        {
            throw new Exception($"Failed to load plugin from: {wrapper.AssemblyLoadedPath}");
        }
       
        wrapper.TryTriggerStartupModule();
        return (assembly, wrapper);
    }
    
    internal void TryTriggerStartupModule()
    {
        Assembly.TryGetTarget(out Assembly? assembly);
        Type[] types = assembly!.GetTypes();
        
        foreach (Type type in types)
        {
            if (!typeof(IModuleInterface).IsAssignableFrom(type))
            {
                continue;
            }
            
            ModuleInterface = (IModuleInterface) Activator.CreateInstance(type)!;
            ModuleInterface.StartupModule();
            break;
        }
    }
    
    internal void TryTriggerShutdownModule()
    {
        if (ModuleInterface == null)
        {
            return;
        }
        
        ModuleInterface.ShutdownModule();
        ModuleInterface = null;
    }
        
    internal void Unload()
    {
        TryTriggerShutdownModule();
        _pluginLoadContext?.Unload();
        _pluginLoadContext = null;
    }
}