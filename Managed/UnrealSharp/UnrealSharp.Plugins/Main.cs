using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using UnrealSharp.Interop;

namespace UnrealSharp.Plugins;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct PluginsCallbacks
{
    public delegate* unmanaged<char*, IntPtr> LoadPlugin;
    public delegate* unmanaged<char*, NativeBool> UnloadPlugin;
}

public static class Main
{
    private static readonly Assembly CoreApiAssembly = typeof(UnrealSharpObject).Assembly;
    private static readonly List<PluginLoadContextWrapper> LoadedPlugins = [];
    private static readonly List<AssemblyName> SharedAssemblies = [];
    private static readonly AssemblyLoadContext MainLoadContext = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()) ?? AssemblyLoadContext.Default;
    private static DllImportResolver? _dllImportResolver;
    
    private sealed class PluginLoadContextWrapper
    {
        private PluginLoadContext? _pluginLoadContext;

        private PluginLoadContextWrapper(PluginLoadContext pluginLoadContext, Assembly assembly)
        {
            _pluginLoadContext = pluginLoadContext;
            Assembly = new WeakReference<Assembly>(assembly);
        }

        public string? AssemblyLoadedPath => _pluginLoadContext?.AssemblyLoadedPath;
        public bool IsCollectible => _pluginLoadContext?.IsCollectible ?? true;
        public bool IsAlive => _pluginLoadContext != null;
        
        // Be careful using this. Any hard reference at the wrong time will prevent the plugin from being unloaded.
        // Thus breaking hot reloading.
        public WeakReference<Assembly> Assembly { get; }

        public static (Assembly, PluginLoadContextWrapper) CreateAndLoadFromAssemblyName(AssemblyName assemblyName, string pluginPath, ICollection<string> sharedAssemblies, AssemblyLoadContext mainLoadContext, bool isCollectible)
        {
            var context = new PluginLoadContext(pluginPath, sharedAssemblies, mainLoadContext, isCollectible);
            var assembly = context.LoadFromAssemblyName(assemblyName);
            var wrapper = new PluginLoadContextWrapper(context, assembly);
            return (assembly, wrapper);
        }
        
        internal void Unload()
        {
            _pluginLoadContext?.Unload();
            _pluginLoadContext = null;
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe NativeBool InitializeUnrealSharp(IntPtr assemblyPath, PluginsCallbacks* pluginCallbacks, ManagedCallbacks* managedCallbacks, IntPtr exportFunctionsPtr)
    {
        try
        {
            AlcReloadCfg.Configure(true);
            
            SetupDllImportResolver(assemblyPath);

            // Initialize plugin and managed callbacks
            *pluginCallbacks = new PluginsCallbacks
            {
                LoadPlugin = &LoadUserAssembly,
                UnloadPlugin = &UnloadProjectPlugin,
            };
                
            // Initialize exported functions
            ExportedFunctionsManager.Initialize(exportFunctionsPtr);

            // Initialize managed callbacks
            *managedCallbacks = ManagedCallbacks.Create();

            Console.WriteLine("UnrealSharp successfully setup!");
            return NativeBool.True;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing C# from Engine: {ex.Message}");
            return NativeBool.False;
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe IntPtr LoadUserAssembly(char* assemblyPath)
    {
        try
        {
            return LoadPlugin(new string(assemblyPath), true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while loading the plugin: {ex.Message}");
        }
        return default;
    }
    
    private static void SetupDllImportResolver(IntPtr assemblyPathPtr)
    {
        _dllImportResolver = new UnrealSharpDllImportResolver(assemblyPathPtr).OnResolveDllImport;
        SharedAssemblies.Add(CoreApiAssembly.GetName());
        NativeLibrary.SetDllImportResolver(CoreApiAssembly, _dllImportResolver);
    }
    
    private static IntPtr LoadPlugin(string assemblyPath, bool isCollectible)
    {
        string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        
        var sharedAssemblies = new List<string>();
        foreach (var sharedAssembly in SharedAssemblies)
        {
            string? sharedAssemblyName = sharedAssembly.Name;
            if (sharedAssemblyName != null)
            {
                sharedAssemblies.Add(sharedAssemblyName);

            }
        }
        
        var (loadedAssembly, newPlugin) = PluginLoadContextWrapper.CreateAndLoadFromAssemblyName(new AssemblyName(assemblyName), assemblyPath, sharedAssemblies, MainLoadContext, isCollectible);

        if (!newPlugin.IsAlive)
        {
            throw new Exception($"Failed to load plugin from: {assemblyPath}");
        }
        
        LoadedPlugins.Add(newPlugin);
        Console.WriteLine($"Successfully loaded plugin: {assemblyName}");
        return GCHandle.ToIntPtr(GcHandleUtilities.AllocateWeakPointer(loadedAssembly));
    }
    
    [UnmanagedCallersOnly]
    private static unsafe NativeBool UnloadProjectPlugin(char* assemblyPath)
    {
        try
        {
            string assemblyPathStr = new string(assemblyPath);
            
            foreach (var plugin in LoadedPlugins)
            {
                if (plugin.AssemblyLoadedPath != assemblyPathStr)
                {
                    continue;
                }
                
                return UnloadPlugin(plugin).ToNativeBool();
            }
            
            throw new Exception($"Failed to find plugin to unload: {assemblyPathStr}");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return NativeBool.False;
        }
    }
    
    private static bool UnloadPlugin(PluginLoadContextWrapper pluginLoadContext)
    {
        try
        {
            if (!pluginLoadContext.IsCollectible)
            {
                throw new InvalidOperationException("Cannot unload a plugin that's not set to IsCollectible.");
            }
            
            FastInvokerManager.FreeAllInvokersForAssembly(pluginLoadContext.Assembly);
            
            Console.WriteLine($"Unloading plugin (Path: {pluginLoadContext.AssemblyLoadedPath}");

            pluginLoadContext.Unload();

            int startTimeMs = Environment.TickCount;
            bool takingTooLong = false;

            while (pluginLoadContext.IsAlive)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();

                if (!pluginLoadContext.IsAlive)
                {
                    break;
                }
                
                int elapsedTimeMs = Environment.TickCount - startTimeMs;

                if (!takingTooLong && elapsedTimeMs >= 200)
                {
                    takingTooLong = true;
                    Console.Error.WriteLine("Unloading assembly took longer than expected.");
                }
                else if (elapsedTimeMs >= 1000)
                {
                    Console.Error.WriteLine("Failed to unload assemblies. Possible causes: Strong GC handles, running threads, etc.");
                    return false;
                }
            }

            LoadedPlugins.Remove(pluginLoadContext);
            Console.WriteLine("Plugin unloaded successfully!");
            return true;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return false;
        }
    }
}