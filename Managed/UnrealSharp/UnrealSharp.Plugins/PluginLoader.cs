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
            
            LogUnrealSharpPlugins.Log($"Successfully loaded plugin: '{assemblyName}' at '{assemblyPath}'");
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

    public static void UnloadPlugin(string assemblyPath)
    {
        const int warnThresholdMs = 200;
        const int timeoutMs = 2000;

        TaskTracker.WaitForAllActiveTasks();
        
        string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        WeakReference? weakAlc = RemovePlugin(assemblyName);

        if (weakAlc == null)
        {
            LogUnrealSharpPlugins.Log($"Plugin {assemblyName} is not loaded or already removed from registry.");
            return;
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
                    LogUnrealSharpPlugins.LogWarning($"Plugin {assemblyName} is taking longer than expected to unload...");
                    hasWarned = true;
                }

                if (stopWatch.ElapsedMilliseconds < timeoutMs)
                {
                    // https://github.com/dotnet/runtime/issues/124876
                    LogUnrealSharpPlugins.LogWarning(
                        $"'{assemblyName}' did not fully unload. " +
                        "A known Visual Studio/Rider debugger issue may be holding strong references to assembly types, preventing the old assembly from being collected. " +
                        "Hot reload will continue to work with some additional memory overhead. " +
                        "Re-attaching the debugger usually releases these references and allows the assembly to be GC'd on the next hot reload.");
                    return;
                }
                
                Thread.Sleep(1);
            }

            LogUnrealSharpPlugins.Log($"{assemblyName} unloaded successfully in {stopWatch.ElapsedMilliseconds}ms.");
        }
        catch (Exception exception)
        {
            LogUnrealSharpPlugins.LogError($"An error occurred while unloading the plugin: {exception}");
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
