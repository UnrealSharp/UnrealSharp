using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using UnrealSharp.Interop;
using UnrealSharp.Logging;

namespace UnrealSharp.Plugins;

public static class Main
{
    internal static readonly Assembly CoreApiAssembly = typeof(UnrealSharpObject).Assembly;
    internal static DllImportResolver _dllImportResolver = null!;
    
    public static readonly AssemblyLoadContext MainLoadContext =
        AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()) ??
        AssemblyLoadContext.Default;

    [UnmanagedCallersOnly]
    private static unsafe NativeBool InitializeUnrealSharp(char* workingDirectoryPath, 
        nint assemblyPath,
        PluginsCallbacks* pluginCallbacks,
        ManagedCallbacks* managedCallbacks,
        nint exportFunctionsPtr)
    {
        try
        {
            AppDomain.CurrentDomain.SetData("APP_CONTEXT_BASE_DIRECTORY", new string(workingDirectoryPath));
            
            _dllImportResolver = new UnrealSharpDllImportResolver(assemblyPath).OnResolveDllImport;
            PluginLoader.SharedAssemblies.Add(CoreApiAssembly);
            PluginLoader.SharedAssemblies.Add(Assembly.GetExecutingAssembly());
            NativeLibrary.SetDllImportResolver(CoreApiAssembly, _dllImportResolver);

            // Initialize plugin and managed callbacks
            *pluginCallbacks = PluginsCallbacks.Create();

            // Initialize exported functions
            ExportedFunctionsManager.Initialize(exportFunctionsPtr);

            // Initialize managed callbacks
            *managedCallbacks = ManagedCallbacks.Create();

            LogUnrealSharp.Log("UnrealSharp successfully setup!");
            return NativeBool.True;
        }
        catch (Exception ex)
        {
            LogUnrealSharp.LogError($"Error initializing UnrealSharp: {ex.Message}");
            return NativeBool.False;
        }
    }
}
