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
            if (!plugin.Load() || plugin.WeakRefAssembly == null || plugin.WeakRefAssembly.Target is not Assembly loadedAssembly)
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
        const int warnThresholdMs = 200;
        const int timeoutMs = 2000;

        TaskTracker.WaitForAllActiveTasks();
        
        string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        WeakReference? alcWeak = RemovePlugin(assemblyName);

        if (alcWeak == null)
        {
            LogUnrealSharpPlugins.Log($"Plugin {assemblyName} is not loaded or already removed from registry.");
            return true;
        }

        try
        {
            LogUnrealSharpPlugins.Log($"Unloading plugin {assemblyName}...");

            Stopwatch stopWatch = Stopwatch.StartNew();
            bool hasWarned = false;

            while (alcWeak.IsAlive)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

                if (!alcWeak.IsAlive)
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
                
                Thread.Sleep(10);
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
