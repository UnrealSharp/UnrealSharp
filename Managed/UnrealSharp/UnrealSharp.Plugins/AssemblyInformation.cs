using System.Reflection;

namespace UnrealSharp.Plugins;

public readonly struct AssemblyInformation(Assembly assembly, PluginLoadContextWrapper pluginLoadContextWrapper, Module? moduleInterface = null)
{
    public Assembly Assembly { get; } = assembly;
    public PluginLoadContextWrapper PluginLoadContextWrapper { get; } = pluginLoadContextWrapper;
    public Module? ModuleInterface { get; } = moduleInterface;

    public bool IsValid => Assembly != null && PluginLoadContextWrapper is { IsAlive: true };
    public string Name => Assembly.GetName().Name!;
    
    public void StartupModule()
    {
        if (ModuleInterface == null)
        {
            return;
        }
        
        ModuleInterface.StartupModule();
    }
    
    public void ShutdownModule()
    {
        if (ModuleInterface == null)
        {
            return;
        }
        
        ModuleInterface.ShutdownModule();
    }
}