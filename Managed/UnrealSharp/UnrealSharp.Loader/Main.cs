using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using UnrealSharp.Interop;

namespace UnrealSharp.Loader;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct PluginsCallbacks
{
    public delegate* unmanaged<char*, IntPtr> LoadAssembly;
    public delegate* unmanaged<char*, NativeBool> UnloadAssembly;
}

public static class Main
{
    private static readonly Assembly CoreApiAssembly = typeof(UnrealSharpObject).Assembly;
    private static readonly List<AssemblyInformation> LoadedPlugins = [];
    private static readonly List<AssemblyName> SharedAssemblies = [];
    private static readonly AssemblyLoadContext MainLoadContext = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()) ?? AssemblyLoadContext.Default;
    private static DllImportResolver? _dllImportResolver;

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
                LoadAssembly = &LoadAssembly,
                UnloadAssembly = &UnloadAssembly,
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
    private static unsafe IntPtr LoadAssembly(char* assemblyPath)
    {
        try
        {
            string assemblyPathString = new string(assemblyPath);
                
            if (!File.Exists(assemblyPathString))
            {
                throw new Exception("Invalid assembly path provided");
            }
                
            return LoadPlugin(assemblyPathString, true);
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
        
        AssemblyInformation assemblyInformation = PluginLoadContextWrapper.CreateAndLoadFromAssemblyName(new AssemblyName(assemblyName), assemblyPath, sharedAssemblies, MainLoadContext, isCollectible);

        if (!assemblyInformation.IsAlive)
        {
            return default;
        }
        
        LoadedPlugins.Add(assemblyInformation);
        Console.WriteLine($"Successfully loaded plugin: {assemblyInformation.Name}");
           
        GCHandle handle = GcHandleUtilities.AllocateWeakPointer(assemblyInformation.GetAssembly());
        return GCHandle.ToIntPtr(handle);
    }
    
    [UnmanagedCallersOnly]
    private static unsafe NativeBool UnloadAssembly(char* assemblyPath)
    {
        try
        {
            string assemblyPathString = new string(assemblyPath);
            
            foreach (var loadedPlugin in LoadedPlugins)
            {
                if (loadedPlugin.Path != assemblyPathString)
                {
                    continue;
                }
                
                return UnloadPlugin(loadedPlugin).ToNativeBool();
            }

            return NativeBool.True;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return NativeBool.False;
        }
    }
    
    private static bool UnloadPlugin(AssemblyInformation assemblyInformation)
    {
        try
        {
            if (!assemblyInformation.IsCollectible)
            {
                Console.Error.WriteLine("Cannot unload a plugin that's not set to IsCollectible.");
                return false;
            }

            assemblyInformation.LoadContextWrapper.Unload();

            int startTimeMs = Environment.TickCount;
            bool takingTooLong = false;

            while (assemblyInformation.IsAlive)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();

                if (!assemblyInformation.IsAlive)
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

            LoadedPlugins.Remove(assemblyInformation);
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