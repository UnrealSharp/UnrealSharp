using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Plugins;

public static class Main
{
    internal static DllImportResolver _dllImportResolver = null!;
    
    public static readonly AssemblyLoadContext MainLoadContext =
        AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()) ??
        AssemblyLoadContext.Default;

    [UnmanagedCallersOnly]
    private static unsafe NativeBool InitializeUnrealSharp(char* workingDirectoryPath, nint assemblyPath, PluginsCallbacks* pluginCallbacks, IntPtr bindsCallbacks)
    {
        try
        {
            AppDomain.CurrentDomain.SetData("APP_CONTEXT_BASE_DIRECTORY", new string(workingDirectoryPath));
            
            PluginLoader.SharedAssemblies.Add(Assembly.GetExecutingAssembly());
            PluginLoader.SharedAssemblies.Add(typeof(NativeBinds).Assembly);

            // Initialize plugin and managed callbacks
            *pluginCallbacks = PluginsCallbacks.Create();
            
            NativeBinds.InitializeNativeBinds(bindsCallbacks);

            LogUnrealSharpPlugins.Log("UnrealSharp successfully setup!");
            return NativeBool.True;
        }
        catch (Exception ex)
        {
            LogUnrealSharpPlugins.LogError($"Error initializing UnrealSharp: {ex.Message}");
            return NativeBool.False;
        }
    }
}
