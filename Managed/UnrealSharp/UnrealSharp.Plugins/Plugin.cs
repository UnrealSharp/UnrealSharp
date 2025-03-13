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
        WeakRefLoadContext = new WeakReference(loadContext, trackResurrection: true);
    }
    
    public AssemblyName AssemblyName { get; }
    public string AssemblyPath;
    
    public PluginLoadContext? LoadContext { get; private set; }
    public WeakReference WeakRefLoadContext { get; private set; }
    
    public WeakReference? WeakRefAssembly { get; private set; }

    private readonly List<IModuleInterface> _moduleInterfaces = [];
    public IReadOnlyList<IModuleInterface> ModuleInterfaces => _moduleInterfaces;

    public bool IsAssemblyAlive
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => WeakRefAssembly != null && WeakRefAssembly.IsAlive;
    }

    public bool IsLoadContextAlive
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => WeakRefLoadContext.IsAlive;
    }

    public bool Load()
    {
        if (LoadContext == null || !WeakRefLoadContext.IsAlive || WeakRefLoadContext.Target is not PluginLoadContext loadContext)
        {
            return false;
        }

        Assembly assembly = loadContext.LoadFromAssemblyName(AssemblyName);
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

        if (LoadContext != null)
        {
            LoadContext.Unload();
            LoadContext = null;
        }
    }
}
