using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using UnrealSharp.Engine.Core.Modules;

namespace UnrealSharp.Plugins;

public class Plugin
{
    public readonly AssemblyName AssemblyName;
    public WeakReference? Assembly { get; private set; }
    
    private AssemblyLoadContext? _loadContext;

    private readonly List<IModuleInterface> _moduleInterfaces = new List<IModuleInterface>();
    private readonly List<Func<IModuleInterface>> _moduleInitFunctions = new List<Func<IModuleInterface>>();
    
    public Plugin(AssemblyName assemblyName, bool isCollectible, string assemblyPath)
    {
        AssemblyName = assemblyName;
        
        Assembly? existingAssembly = AssemblyCache.GetUniqueAssembly(assemblyName.Name!);
        if (existingAssembly != null)
        {
            AssemblyLoadContext? existingLoadContext = AssemblyLoadContext.GetLoadContext(existingAssembly);
            
            if (existingLoadContext == null)
            {
                throw new InvalidOperationException($"Unable to determine load context for existing assembly {assemblyName}.");
            }
            
            if (existingLoadContext.IsCollectible)
            {
                throw new InvalidOperationException($"Shared collectible context detected for {assemblyName}. '{existingLoadContext.Name}' is collectible.");
            }
            
            _loadContext = existingLoadContext;
        }
        else
        {
            _loadContext = new PluginLoadContext(assemblyName.Name!, new AssemblyDependencyResolver(assemblyPath), isCollectible);
        }
    }

    public void AddModuleInterfaceInit(Func<IModuleInterface> initFunction)
    {
        _moduleInitFunctions.Add(initFunction);
    }

    public bool Load()
    {
        if (_loadContext == null || Assembly != null)
        {
            return false;
        }
        
        Assembly assembly = _loadContext.LoadFromAssemblyName(AssemblyName);
        Assembly = new WeakReference(assembly);
        
        RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        
        StartupModule();
        return true;
    }
    
    public T GetModule<T>() where T : class, IModuleInterface
    {
        T? module = _moduleInterfaces.OfType<T>().FirstOrDefault();

        if (module == null)
        {
            throw new Exception($"Module of type '{typeof(T).Name}' not found.");
        }

        return module;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public WeakReference Unload()
    {
        ShutdownModule();
        
        AssemblyCache.RemoveAssembly(AssemblyName.Name!);
        Assembly = null;
        
        WeakReference loadContextWeak = new WeakReference(_loadContext);
        _loadContext!.Unload();
        _loadContext = null;
        
        return loadContextWeak;
    }

    public void StartupModule()
    {
        _moduleInterfaces.Capacity = _moduleInitFunctions.Count;
        
        foreach (Func<IModuleInterface> moduleInterfaceInitFunc in _moduleInitFunctions)
        {
            IModuleInterface moduleInterface = moduleInterfaceInitFunc();
            _moduleInterfaces.Add(moduleInterface);
            
            moduleInterface.StartupModule();
        }
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ShutdownModule()
    {
        foreach (IModuleInterface moduleInterface in _moduleInterfaces)
        {
            moduleInterface.ShutdownModule();
        }

        _moduleInterfaces.Clear();
        _moduleInitFunctions.Clear();
    }

    public override string ToString()
    {
        return AssemblyName.ToString();
    }
}
