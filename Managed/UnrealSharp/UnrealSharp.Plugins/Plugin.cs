using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using UnrealSharp.Engine.Core.Modules;

namespace UnrealSharp.Plugins;

public class Plugin
{
    public Plugin(AssemblyName assemblyName, bool isCollectible, string assemblyPath)
    {
        AssemblyName = assemblyName;
        _loadContext = new PluginLoadContext(assemblyName.Name! + "_AssemblyLoadContext", new AssemblyDependencyResolver(assemblyPath), isCollectible);
        _moduleInterfaces = new List<IModuleInterface>();
    }
    
    public readonly AssemblyName AssemblyName;
    public WeakReference? WeakRefAssembly { get; private set; }
    
    private PluginLoadContext? _loadContext;
    private readonly List<IModuleInterface> _moduleInterfaces;

    public bool Load()
    {
        if (_loadContext == null || (WeakRefAssembly != null && WeakRefAssembly.IsAlive))
        {
            return false;
        }
        
        Assembly assembly = _loadContext.LoadFromAssemblyName(AssemblyName);
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
            _moduleInterfaces.Add(moduleInterface);
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public WeakReference Unload()
    {
        ShutdownModule();

        if (_loadContext == null)
        {
            throw new InvalidOperationException("Cannot unload a plugin that is not loaded.");
        }
        
        PluginLoadContext.RemoveAssemblyFromCache(AssemblyName.Name!);
        
        WeakReference loadContextWeak = new WeakReference(_loadContext);
        
        _loadContext.Unload();
        _loadContext = null;
        WeakRefAssembly = null;
        
        return loadContextWeak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ShutdownModule()
    {
        foreach (IModuleInterface moduleInterface in _moduleInterfaces)
        {
            moduleInterface.ShutdownModule();
        }

        _moduleInterfaces.Clear();
    }

    public override string ToString()
    {
        return AssemblyName.ToString();
    }
}
