using System.Reflection;
using System.Runtime.Loader;
using LanguageExt;
using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Plugins;

public static class PluginLoader
{
    public static readonly List<Assembly> SharedAssemblies = [];
    
    private static readonly List<Plugin> _loadedPlugins = [];
    public static IReadOnlyList<Plugin> LoadedPlugins => _loadedPlugins;
    
    static PluginLoader()
    {
        SharedAssemblies.Add(typeof(PluginLoader).Assembly);
        SharedAssemblies.Add(typeof(NativeBinds).Assembly);
        SharedAssemblies.Add(typeof(UnrealSharpObject).Assembly);
        SharedAssemblies.Add(typeof(UnrealSharpModule).Assembly);
        SharedAssemblies.Add(typeof(Option<>).Assembly);
    }

    public static Assembly? LoadPlugin(string assemblyPath, bool isCollectible)
    {
        try
        {
            AssemblyName assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(assemblyPath));

            foreach (var loadedPlugin in _loadedPlugins)
            {
                if (!loadedPlugin.IsAssemblyAlive)
                {
                    continue;
                }
                
                if (loadedPlugin.WeakRefAssembly?.Target is not Assembly assembly)
                {
                    continue;
                }

                if (assembly.GetName() != assemblyName)
                {
                    continue;
                }

                LogUnrealSharpPlugins.Log($"Plugin {assemblyName} is already loaded.");
                return assembly;
            }

            // Just for debugging
            string pluginLoadContextName = assemblyName.Name! + "_AssemblyLoadContext";
            
            PluginLoadContext pluginLoadContext = new PluginLoadContext(pluginLoadContextName, new AssemblyDependencyResolver(assemblyPath), isCollectible);
            Plugin plugin = new Plugin(assemblyName, pluginLoadContext, assemblyPath);
  
            if (plugin.Load() && plugin.WeakRefAssembly != null && plugin.WeakRefAssembly.Target is Assembly loadedAssembly)
            {
                _loadedPlugins.Add(plugin);
                LogUnrealSharpPlugins.Log($"Successfully loaded plugin: {assemblyName}");
                return loadedAssembly;
            }
            
            throw new InvalidOperationException($"Failed to load plugin: {assemblyName}");
        }
        catch (Exception ex)
        {
            LogUnrealSharpPlugins.LogError($"An error occurred while loading the plugin: {ex.Message}");
        }

        return null;
    }

    public static bool UnloadPlugin(string assemblyPath)
    {
        string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

        Plugin? pluginToUnload = null;
        foreach (Plugin loadedPlugin in _loadedPlugins)
        {
            // Trying to resolve the weakptr to the assembly here will cause unload issues, so we compare names instead
            if (!loadedPlugin.IsAssemblyAlive || loadedPlugin.AssemblyName.Name != assemblyName)
            {
                continue;
            }
            
            pluginToUnload = loadedPlugin;
            break;
        }

        if (pluginToUnload == null)
        {
            LogUnrealSharpPlugins.Log($"Plugin {assemblyName} is not loaded or already unloaded. No unload required.");
            return true;
        }
        
        try
        {
            LogUnrealSharpPlugins.Log($"Unloading plugin {assemblyName}...");
            
            pluginToUnload.Unload();
            _loadedPlugins.Remove(pluginToUnload);

            int startTimeMs = Environment.TickCount;
            bool takingTooLong = false;

            while (pluginToUnload.IsAssemblyAlive)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();

                if (!pluginToUnload.IsAssemblyAlive)
                {
                    pluginToUnload.PostUnload();
                    break;
                }

                int elapsedTimeMs = Environment.TickCount - startTimeMs;
                
                if (!takingTooLong && elapsedTimeMs >= 200)
                {
                    takingTooLong = true;
                    LogUnrealSharpPlugins.LogError($"Unloading {assemblyName} is taking longer than expected...");
                }
                else if (elapsedTimeMs >= 1000)
                {
                    throw new InvalidOperationException($"Failed to unload {assemblyName}. Possible causes: Strong GC handles, running threads, etc.");
                }
            }

            LogUnrealSharpPlugins.Log($"{assemblyName} unloaded successfully!");
            return true;
        }
        catch (Exception e)
        {
            LogUnrealSharpPlugins.LogError($"An error occurred while unloading the plugin: {e.Message}");
            return false;
        }
    }
    
    public static Plugin? FindPluginByName(string assemblyName)
    {
        foreach (Plugin loadedPlugin in _loadedPlugins)
        {
            if (loadedPlugin.AssemblyName.Name == assemblyName)
            {
                return loadedPlugin;
            }
        }

        return null;
    }
}
