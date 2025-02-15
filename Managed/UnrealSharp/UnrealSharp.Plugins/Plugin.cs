using System.Reflection;
using UnrealSharp.Engine.Core.Modules;

namespace UnrealSharp.Plugins;

public class Plugin
{
    public Plugin(AssemblyName assemblyName, WeakReference weakRefLoadContext, string assemblyPath)
    {
        AssemblyName = assemblyName;
        WeakRefLoadContext = weakRefLoadContext;
        AssemblyPath = assemblyPath;
    }
    
    public AssemblyName AssemblyName { get; }
    public string AssemblyPath;
    
    public WeakReference? WeakRefLoadContext { get; }
    public WeakReference? WeakRefAssembly { get; private set; }

    private readonly List<IModuleInterface>? _moduleInterfaces = [];
    public IReadOnlyList<IModuleInterface>? ModuleInterfaces => _moduleInterfaces;

    public bool IsAssemblyAlive => WeakRefAssembly != null && WeakRefAssembly.IsAlive;
    public bool IsLoadContextAlive => WeakRefLoadContext != null && WeakRefLoadContext.IsAlive;

    public bool Load()
    {
        if (WeakRefLoadContext == null || !WeakRefLoadContext.IsAlive || WeakRefLoadContext.Target is not PluginLoadContext loadContext)
        {
            return false;
        }

        Assembly assembly = loadContext.LoadFromAssemblyName(AssemblyName);
        WeakRefAssembly = new WeakReference(assembly);

        if (_moduleInterfaces == null)
        {
            return true;
        }
        
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
            _moduleInterfaces.Add(moduleInterface);
        }

        return true;
    }

    public void Unload()
    {
        if (_moduleInterfaces != null)
        {
            foreach (IModuleInterface moduleInterface in _moduleInterfaces)
            {
                moduleInterface.ShutdownModule();
            }

            _moduleInterfaces.Clear();
        }

        if (WeakRefLoadContext != null && WeakRefLoadContext.IsAlive && WeakRefLoadContext.Target is PluginLoadContext loadContext)
        {
            loadContext.Unload();
        }
    }
}
