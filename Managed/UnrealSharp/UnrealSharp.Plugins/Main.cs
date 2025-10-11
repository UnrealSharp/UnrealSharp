﻿using System.Runtime.InteropServices;
using Microsoft.Build.Locator;
using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Plugins;

public static class Main
{
    [UnmanagedCallersOnly]
    private static unsafe NativeBool InitializeUnrealSharp(char* workingDirectoryPath, nint assemblyPath, PluginsCallbacks* pluginCallbacks, IntPtr bindsCallbacks, IntPtr managedCallbacks)
    {
        try
        {
            #if WITH_EDITOR
            IEnumerable<VisualStudioInstance> instances = MSBuildLocator.QueryVisualStudioInstances();
            VisualStudioInstance? visualStudioInstance = instances.OrderByDescending(i => i.Version).FirstOrDefault();
            
            if (visualStudioInstance is not null)
            {
                MSBuildLocator.RegisterInstance(visualStudioInstance);
            }
            else
            {
                MSBuildLocator.RegisterDefaults();
            }
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
