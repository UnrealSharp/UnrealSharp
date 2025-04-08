using System.Reflection;
using System.Runtime.CompilerServices;
using UnrealSharp.Engine.Core.Modules;

namespace UnrealSharp.Plugins;

public class Plugin
{
    public Plugin(AssemblyName assemblyName, PluginLoadContext loadContext, string assemblyPath)
    {
        AssemblyName = assemblyName;
        AssemblyPath = assemblyPath;
        
        LoadContext = loadContext;
        WeakRefLoadContext = new WeakReference(loadContext);
    }
    
    public AssemblyName AssemblyName { get; set; }
    public string AssemblyPath;
    
    public PluginLoadContext? LoadContext { get; private set; }
    public WeakReference? WeakRefLoadContext { get ; private set; }
    
    public WeakReference? WeakRefAssembly { get; private set; }
    public List<IModuleInterface> ModuleInterfaces { get; } = [];

    public bool IsAssemblyAlive
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => WeakRefAssembly != null && WeakRefAssembly.IsAlive;
    }

    public bool Load()
    {
        if (LoadContext == null || (WeakRefAssembly != null && WeakRefAssembly.IsAlive))
        {
            return false;
        }
        
        Assembly assembly = LoadContext.LoadFromAssemblyName(AssemblyName);
        WeakRefAssembly = new WeakReference(assembly);
        
        Type[] types = assembly.GetTypes();
            
        foreach (Type type in types)
        {
            if (type == typeof(IModuleInterface) || !typeof(IModuleInterface).IsAssignableFrom(type))
            {
                continue;
            }

            if (Activator.CreateInstance(type) is not IModuleInterface moduleInterface)
            {
                continue;
            }

            moduleInterface.StartupModule();
            ModuleInterfaces.Add(moduleInterface);
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Unload()
    {
        ShutdownModule();

        if (LoadContext == null)
        {
            return;
        }
        
        LoadContext.Unload();
        LoadContext = null;
        WeakRefLoadContext = null;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void PostUnload()
    {
        WeakRefAssembly = null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ShutdownModule()
    {
        foreach (IModuleInterface moduleInterface in ModuleInterfaces)
        {
            moduleInterface.ShutdownModule();
        }

        ModuleInterfaces.Clear();
    }
}
