using System.Reflection;
using UnrealSharp.Engine.Core.Modules;

namespace UnrealSharp.Plugins;

public class Plugin(AssemblyName assemblyName, WeakReference weakRefLoadContext)
{
    public AssemblyName AssemblyName { get; } = assemblyName;
    public WeakReference? WeakRefLoadContext { get; private set; } = weakRefLoadContext;
    public WeakReference? WeakRefAssembly { get; private set; }

    private List<WeakReference>? weakRefModuleInterfaces = [];
    public IReadOnlyList<WeakReference>? WeakRefModuleInterfaces => weakRefModuleInterfaces;

    public bool IsAssemblyAlive => WeakRefAssembly != null && WeakRefAssembly.IsAlive;
    public bool IsLoadContextAlive => WeakRefLoadContext != null && WeakRefLoadContext.IsAlive;

    public bool Load()
    {
        if (WeakRefLoadContext == null) return false;
        if (!WeakRefLoadContext.IsAlive) return false;
        if (WeakRefLoadContext.Target is not PluginLoadContext loadContext) return false;

        Assembly assembly = loadContext.LoadFromAssemblyName(AssemblyName);
        WeakRefAssembly = new WeakReference(assembly);

        if (weakRefModuleInterfaces != null)
        {
            var moduleInterfaceTypes = assembly.GetTypes()
                .Where(t => typeof(IModuleInterface).IsAssignableFrom(t));

            foreach (var moduleInterfaceType in moduleInterfaceTypes)
            {
                if (Activator.CreateInstance(moduleInterfaceType) is not IModuleInterface moduleInterface) continue;

                moduleInterface.StartupModule();

                weakRefModuleInterfaces.Add(new WeakReference(moduleInterface));
            }
        }

        return true;
    }

    public void Unload()
    {
        if (weakRefModuleInterfaces != null)
        {
            foreach (var weakRefModuleInterface in weakRefModuleInterfaces)
            {
                if (!weakRefModuleInterface.IsAlive) continue;
                if (weakRefModuleInterface.Target is not IModuleInterface moduleInterface) continue;

                moduleInterface.ShutdownModule();
            }

            weakRefModuleInterfaces.Clear();
        }

        if (WeakRefLoadContext != null && WeakRefLoadContext.IsAlive && WeakRefLoadContext.Target is PluginLoadContext loadContext)
        {
            loadContext.Unload();
        }
    }
}
