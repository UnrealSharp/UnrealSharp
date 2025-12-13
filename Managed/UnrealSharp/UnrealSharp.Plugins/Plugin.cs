using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using UnrealSharp.Engine.Core.Modules;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.Plugins;

public class Plugin
{
    public Plugin(AssemblyName assemblyName, bool isCollectible, string assemblyPath)
    {
        AssemblyName = assemblyName;
        AssemblyPath = assemblyPath;
        
        string pluginLoadContextName = assemblyName.Name! + "_AssemblyLoadContext";
        LoadContext = new PluginLoadContext(pluginLoadContextName, new AssemblyDependencyResolver(assemblyPath), isCollectible);
        WeakRefLoadContext = new WeakReference(LoadContext);
    }
    
    public AssemblyName AssemblyName { get; }
    public string AssemblyPath;
    
    public PluginLoadContext? LoadContext { get; private set; }
    public WeakReference? WeakRefLoadContext { get ; }
    
    public WeakReference? WeakRefAssembly { get; private set; }
    public List<IModuleInterface> ModuleInterfaces { get; } = [];

    public bool IsLoadContextAlive
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => WeakRefLoadContext != null && WeakRefLoadContext.IsAlive;
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
        
        PluginLoadContext.RemoveAssemblyFromCache(AssemblyName.Name);
        
        LoadContext.Unload();
        LoadContext = null;
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
