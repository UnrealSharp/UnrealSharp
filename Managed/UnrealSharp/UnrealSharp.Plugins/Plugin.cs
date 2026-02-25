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
    private readonly List<IModuleInterface> _moduleInterfaces;
    private readonly AssemblyDependencyResolver _resolver;
    
    public Plugin(AssemblyName assemblyName, bool isCollectible, string assemblyPath)
    {
        AssemblyName = assemblyName;
        _moduleInterfaces = new List<IModuleInterface>();
        _resolver = new AssemblyDependencyResolver(assemblyPath);
        
        Assembly? existingAssembly = AssemblyCache.GetAssembly(assemblyName.Name!);
        if (existingAssembly != null)
        {
            AssemblyLoadContext? existingLoadContext = AssemblyLoadContext.GetLoadContext(existingAssembly);
            
            if (existingLoadContext == null)
            {
                throw new InvalidOperationException($"Unable to determine load context for existing assembly {assemblyName}.");
            }
            
            if (existingLoadContext.IsCollectible)
            {
                throw new InvalidOperationException($"Shared collectible context detected for {assemblyName}.");
            }
            
            _loadContext = existingLoadContext;
        }
        else
        {
            _loadContext = new AssemblyLoadContext(assemblyName.Name, isCollectible);
            _loadContext.Resolving += ResolveAssembly;
        }
    }

    private Assembly? ResolveAssembly(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName.Name))
        {
            return null;
        }
        
        Assembly? loadedAssembly = AssemblyCache.GetAssembly(assemblyName.Name!);
        if (loadedAssembly != null)
        {
            return loadedAssembly;
        }

        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

        if (string.IsNullOrEmpty(assemblyPath))
        {
            return null;
        }

        using FileStream assemblyFile = File.Open(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");

        Assembly? newAssembly;
        if (!File.Exists(pdbPath))
        {
            newAssembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);
        }
        else
        {
            using FileStream pdbFile = File.Open(pdbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            newAssembly = assemblyLoadContext.LoadFromStream(assemblyFile, pdbFile);
        }
        
        AssemblyCache.AddAssembly(newAssembly);
        return newAssembly;
    }

    public bool Load()
    {
        if (_loadContext == null || (Assembly != null && Assembly.IsAlive))
        {
            return false;
        }
        
        Assembly assembly = _loadContext.LoadFromAssemblyName(AssemblyName);
        Assembly = new WeakReference(assembly);
        
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
        
        AssemblyCache.RemoveAssembly(AssemblyName.Name!);
        Assembly = null;
        
        WeakReference loadContextWeak = new WeakReference(_loadContext);
        _loadContext!.Resolving -= ResolveAssembly;
        _loadContext.Unload();
        _loadContext = null;
        
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
