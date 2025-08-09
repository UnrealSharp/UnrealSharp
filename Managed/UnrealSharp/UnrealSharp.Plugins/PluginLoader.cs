using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace UnrealSharp.Plugins;

public static class PluginLoader
{
    public static readonly List<Plugin> LoadedPlugins = [];

    public static Assembly? LoadPlugin(string assemblyPath, bool isCollectible)
    {
        try
        {
            AssemblyName assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(assemblyPath));

            foreach (Plugin loadedPlugin in LoadedPlugins)
            {
                if (!loadedPlugin.IsLoadContextAlive)
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
            
            Plugin plugin = new Plugin(assemblyName, isCollectible, assemblyPath);
            if (plugin.Load() && plugin.WeakRefAssembly != null && plugin.WeakRefAssembly.Target is Assembly loadedAssembly)
            {
                LoadedPlugins.Add(plugin);
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
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference? RemovePlugin(string assemblyName)
    {
        foreach (Plugin loadedPlugin in LoadedPlugins)
        {
            // Trying to resolve the weakptr to the assembly here will cause unload issues, so we compare names instead
            if (!loadedPlugin.IsLoadContextAlive || loadedPlugin.AssemblyName.Name != assemblyName)
            {
                continue;
            }
            
            loadedPlugin.Unload();
            LoadedPlugins.Remove(loadedPlugin);
            return loadedPlugin.WeakRefLoadContext;
        }
        
        return null;
    }

    public static bool UnloadPlugin(string assemblyPath)
    {
        string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        WeakReference? assemblyLoadContext = RemovePlugin(assemblyName);
        
        if (assemblyLoadContext == null)
        {
            LogUnrealSharpPlugins.Log($"Plugin {assemblyName} is not loaded or already unloaded.");
            return true;
        }
        
        try
        {
            LogUnrealSharpPlugins.Log($"Unloading plugin {assemblyName}...");

            int startTimeMs = Environment.TickCount;
            bool takingTooLong = false;

            while (assemblyLoadContext.IsAlive)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();

                if (!assemblyLoadContext.IsAlive)
                {
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
        foreach (Plugin loadedPlugin in LoadedPlugins)
        {
            if (loadedPlugin.AssemblyName.Name == assemblyName)
            {
                return loadedPlugin;
            }
        }

        return null;
    }
}
