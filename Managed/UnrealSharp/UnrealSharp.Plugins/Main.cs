using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.Build.Locator;
using UnrealSharp.Binds;
using UnrealSharp.Core;
using UnrealSharp.Shared;

namespace UnrealSharp.Plugins;

public static class Main
{
    internal static DllImportResolver _dllImportResolver = null!;
    
    public static readonly AssemblyLoadContext MainLoadContext =
        AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()) ??
        AssemblyLoadContext.Default;

    [UnmanagedCallersOnly]
    private static unsafe NativeBool InitializeUnrealSharp(char* workingDirectoryPath, nint assemblyPath, PluginsCallbacks* pluginCallbacks, IntPtr bindsCallbacks, IntPtr managedCallbacks)
    {
        try
        {
            #if !PACKAGE
            string dotnetSdk = DotNetUtilities.GetLatestDotNetSdkPath();
            MSBuildLocator.RegisterMSBuildPath(dotnetSdk);
            #endif
            
            AppDomain.CurrentDomain.SetData("APP_CONTEXT_BASE_DIRECTORY", new string(workingDirectoryPath));

            // Initialize plugin and managed callbacks
            *pluginCallbacks = PluginsCallbacks.Create();
            
            NativeBinds.InitializeNativeBinds(bindsCallbacks);
            ManagedCallbacks.Initialize(managedCallbacks);

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
