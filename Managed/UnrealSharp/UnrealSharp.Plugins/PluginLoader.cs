using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using UnrealSharp.Core;

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
                if (loadedPlugin.Assembly?.Target is not Assembly assembly)
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
            if (!plugin.Load() || plugin.Assembly == null || plugin.Assembly.Target is not Assembly loadedAssembly)
            {
                throw new InvalidOperationException($"Failed to load plugin: {assemblyName}");
            }

            LoadedPlugins.Add(plugin);

            if (!StartupJobManager.HasJobs(loadedAssembly))
            {
                // Sometimes the module initializer doesn't run automatically, so we force it here
                RuntimeHelpers.RunModuleConstructor(loadedAssembly.ManifestModule.ModuleHandle);
            }
            
            StartupJobManager.RunForAssembly(loadedAssembly);
            
            LogUnrealSharpPlugins.Log($"Successfully loaded plugin: {assemblyName}");
            return loadedAssembly;
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
        WeakReference? weakRefLoadContext = null;
        foreach (Plugin loadedPlugin in LoadedPlugins)
        {
            if (loadedPlugin.AssemblyName.Name != assemblyName)
            {
                continue;
            }
            
            LoadedPlugins.Remove(loadedPlugin);
            weakRefLoadContext = loadedPlugin.Unload();
            break;
        }
        
        return weakRefLoadContext;
    }

    public static bool UnloadPlugin(string assemblyPath)
    {
        const int warnThresholdMs = 200;
        const int timeoutMs = 2000;

        TaskTracker.WaitForAllActiveTasks();
        
        string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        WeakReference? weakAlc = RemovePlugin(assemblyName);

        if (weakAlc == null)
        {
            LogUnrealSharpPlugins.Log($"Plugin {assemblyName} is not loaded or already removed from registry.");
            return true;
        }

        try
        {
            LogUnrealSharpPlugins.Log($"Unloading plugin {assemblyName}...");

            Stopwatch stopWatch = Stopwatch.StartNew();
            bool hasWarned = false;

            while (weakAlc.IsAlive)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

                if (!weakAlc.IsAlive)
                {
                    break;
                }
                
                if (!hasWarned && stopWatch.ElapsedMilliseconds >= warnThresholdMs)
                {
                    hasWarned = true;
                    LogUnrealSharpPlugins.LogError($"Unloading {assemblyName} is taking longer than expected...");
                }

                if (stopWatch.ElapsedMilliseconds >= timeoutMs)
                {
                    LogUnrealSharpPlugins.LogError($"Failed to unload {assemblyName} within {timeoutMs}ms. Common causes: static references, GCHandles, background threads.");
                    return false;
                }
            }

            LogUnrealSharpPlugins.Log($"{assemblyName} unloaded successfully in {stopWatch.ElapsedMilliseconds}ms.");
            return true;
        }
        catch (Exception exception)
        {
            LogUnrealSharpPlugins.LogError($"An error occurred while unloading the plugin: {exception}");
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
