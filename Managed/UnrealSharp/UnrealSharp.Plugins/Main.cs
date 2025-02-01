using System.Reflection;
using System.Runtime.InteropServices;
using UnrealSharp.Interop;
using UnrealSharp.Logging;

namespace UnrealSharp.Plugins;

public static class Main
{
    private static readonly Assembly CoreApiAssembly = typeof(UnrealSharpObject).Assembly;
    private static DllImportResolver? _dllImportResolver;

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

            SetupDllImportResolver(assemblyPath);

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

    private static void SetupDllImportResolver(nint assemblyPathPtr)
    {
        _dllImportResolver = new UnrealSharpDllImportResolver(assemblyPathPtr).OnResolveDllImport;
        PluginLoader.SharedAssemblies.Add(CoreApiAssembly);
        NativeLibrary.SetDllImportResolver(CoreApiAssembly, _dllImportResolver);
    }
}
