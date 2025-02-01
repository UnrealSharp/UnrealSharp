using System.Reflection;
using System.Runtime.Loader;
using UnrealSharp.Logging;

namespace UnrealSharp.Plugins;

public static class PluginLoader
{
    public static List<Assembly> SharedAssemblies = [];

    private static List<Plugin> loadedPlugins = [];
    public static IReadOnlyList<Plugin> LoadedPlugins => loadedPlugins;

    public static WeakReference? LoadPlugin(string assemblyPath, bool isCollectible)
    {
        try
        {
            var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(assemblyPath));

            foreach (var loadedPlugin in loadedPlugins)
            {
                if (!loadedPlugin.IsAssemblyAlive) continue;
                if (loadedPlugin.WeakRefAssembly?.Target is not Assembly assembly) continue;

                if (assembly.GetName() != assemblyName) continue;

                LogUnrealSharp.Log($"Plugin {assemblyName} is already loaded.");
                return loadedPlugin.WeakRefAssembly;
            }

            var pluginLoadContext = new PluginLoadContext(new AssemblyDependencyResolver(assemblyPath), isCollectible);
            var weakRefPluginLoadContext = new WeakReference(pluginLoadContext);

            var plugin = new Plugin(assemblyName, weakRefPluginLoadContext);
  
            if (plugin.Load())
            {
                loadedPlugins.Add(plugin);

                LogUnrealSharp.Log($"Successfully loaded plugin: {assemblyName}");
                return plugin.WeakRefAssembly;
            }
            else
            {
                LogUnrealSharp.LogError($"Failed to load plugin: {assemblyName}");
            }

            return default;
        }
        catch (Exception ex)
        {
            LogUnrealSharp.LogError($"An error occurred while loading the plugin: {ex.Message}");
        }

        return default;
    }

    public static bool UnloadPlugin(string assemblyPath)
    {
        string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

        Plugin? pluginToUnload = null;
        foreach (Plugin loadedPlugin in loadedPlugins)
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
            return false;
        }
        
        try
        {
            LogUnrealSharp.Log($"Unloading plugin {assemblyName}...");
            pluginToUnload.Unload();

            int startTimeMs = Environment.TickCount;
            bool takingTooLong = false;

            while (pluginToUnload.IsAssemblyAlive && pluginToUnload.IsLoadContextAlive)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();

                if (!pluginToUnload.IsAssemblyAlive && !pluginToUnload.IsLoadContextAlive)
                {
                    break;
                }

                int elapsedTimeMs = Environment.TickCount - startTimeMs;

                if (!takingTooLong && elapsedTimeMs >= 200)
                {
                    takingTooLong = true;
                    LogUnrealSharp.LogError("Unloading assembly is taking longer than expected...");
                }
                else if (elapsedTimeMs >= 1000)
                {
                    throw new InvalidOperationException("Failed to unload assemblies. Possible causes: Strong GC handles, running threads, etc.");
                }
            }

            loadedPlugins.Remove(pluginToUnload);

            LogUnrealSharp.Log($"{assemblyName} unloaded successfully!");
            return true;
        }
        catch (Exception e)
        {
            LogUnrealSharp.LogError($"An error occurred while unloading the plugin: {e.Message}");
            return false;
        }
    }
}
