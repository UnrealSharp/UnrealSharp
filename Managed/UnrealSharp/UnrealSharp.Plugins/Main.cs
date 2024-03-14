using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace UnrealSharp.Plugins;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct PluginsCallbacks
{
    public delegate* unmanaged<char*, IntPtr, IntPtr, IntPtr> LoadPlugin;
    public delegate* unmanaged<GCHandle, NativeBool> UnloadPlugin;
}

public static class Main
{
    private static readonly List<AssemblyInformation> LoadedAssemblies = [];
    private static readonly List<AssemblyName> SharedAssemblies = [];
    private static readonly AssemblyLoadContext MainLoadContext = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()) ?? AssemblyLoadContext.Default;
    private static DllImportResolver? _dllImportResolver;

    [UnmanagedCallersOnly]
    private static unsafe NativeBool InitializeUnrealSharp(IntPtr assemblyPath, PluginsCallbacks* pluginCallbacks, IntPtr exportFunctionsPtr)
    {
        try
        {
            // Initialize plugin and managed callbacks
            *pluginCallbacks = new PluginsCallbacks
            {
                LoadPlugin = &LoadUserAssembly,
                UnloadPlugin = &UnloadProjectPlugin,
            };

            Console.WriteLine("UnrealSharp successfully setup!");
            return NativeBool.True;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing UnrealSharp from Engine: {ex.Message}");
            return NativeBool.False;
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe IntPtr LoadUserAssembly(char* assemblyPath, IntPtr unmanagedCallbacks, IntPtr exportFunctionsPtr)
    {
        try
        {
            string assemblyPathString = new string(assemblyPath);
                
            if (!File.Exists(assemblyPathString))
            {
                throw new Exception("Invalid assembly path provided");
            }
                
            AssemblyInformation newAssembly = LoadAssembly(assemblyPathString, true, unmanagedCallbacks, exportFunctionsPtr);

            if (newAssembly.IsValid)
            {
                return GCHandle.ToIntPtr(GcHandleUtilities.AllocateWeakPointer(newAssembly.Assembly));
            }

            throw new Exception($"Failed to load assembly from: {assemblyPathString}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while loading the assembly: {ex.Message}");
        }
        return default;
    }
    
    private static AssemblyInformation LoadAssembly(string assemblyPath, bool isCollectible, IntPtr unmanagedCallbacks, IntPtr exportFunctionsPtr)
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
        
        var assemblyInformation = PluginLoadContextWrapper.CreateAndLoadFromAssemblyName(new AssemblyName(assemblyName), assemblyPath, sharedAssemblies, MainLoadContext, isCollectible);

        if (!assemblyInformation.IsValid)
        {
            return default;
        }
        
        LoadedAssemblies.Add(assemblyInformation);
        
        // See if there are any entry points in the assembly for us to call
        MethodInfo? methodInfo = PluginsHelper.FindEntryPointMethod(assemblyInformation.Assembly);
        
        if (methodInfo != null)
        {
            methodInfo.Invoke(null, [unmanagedCallbacks, exportFunctionsPtr]);
        }
        
        Console.WriteLine($"Successfully loaded assembly: {assemblyInformation.Name}");
        return assemblyInformation;
    }
    
    [UnmanagedCallersOnly]
    private static NativeBool UnloadProjectPlugin(GCHandle handle)
    {
        try
        {
            if (handle.Target == null)
            {
                throw new Exception("Invalid assembly handle provided");
            }
            
            Assembly? assembly = (Assembly) handle.Target;
            
            if (assembly == null)
            {
                throw new Exception("Invalid assembly handle provided");
            }
            
            foreach (var loadedAssembly in LoadedAssemblies)
            {
                if (loadedAssembly.Assembly == assembly && UnloadAssembly(loadedAssembly))
                {
                    LoadedAssemblies.Remove(loadedAssembly);
                    Console.WriteLine($"Successfully unloaded assembly {loadedAssembly.Name}");
                    return NativeBool.True;
                }
            }
            
            throw new Exception("Failed to find the assembly to unload");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return NativeBool.False;
        }
    }
    
    private static bool UnloadAssembly(AssemblyInformation assemblyInformation)
    {
        try
        {
            if (!assemblyInformation.PluginLoadContextWrapper.IsCollectible)
            {
                Console.Error.WriteLine("Cannot unload a assembly that's not set to IsCollectible.");
                return false;
            }
            
            Console.WriteLine($"Unloading assembly (Path: {assemblyInformation.PluginLoadContextWrapper.AssemblyLoadedPath}");

            assemblyInformation.PluginLoadContextWrapper.Unload();

            int startTimeMs = Environment.TickCount;
            bool takingTooLong = false;

            while (assemblyInformation.IsValid)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();

                if (!assemblyInformation.IsValid)
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
            
            return true;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return false;
        }
    }
}