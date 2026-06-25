using System.Runtime.InteropServices;
using Microsoft.Build.Locator;
using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Plugins;

public static class Main
{
    [UnmanagedCallersOnly]
    private static unsafe NativeBool InitializeJitRuntime(char* workingDirectoryPath, 
        PluginsCallbacks* pluginCallbacks, 
        IntPtr bindsCallbacks, 
        IntPtr managedCallbacks)
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
            
            PluginsCallbacks.Initialize(pluginCallbacks);
            ManagedCallbacks.Initialize(managedCallbacks);
            NativeBinds.Initialize(bindsCallbacks);

            Console.WriteLine("UnrealSharp initialized successfully.");
            return NativeBool.True;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            return NativeBool.False;
        }
    }
}
