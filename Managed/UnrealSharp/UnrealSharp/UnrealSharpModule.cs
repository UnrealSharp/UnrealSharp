using UnrealSharp.Interop;
using UnrealSharp.Plugins;

namespace UnrealSharp;

public class UnrealSharpModule : Module
{
    public UnrealSharpModule(IntPtr unmanagedCallbacks, IntPtr exportFunctionsPtr)
    {
        ExportedFunctionsManager.Initialize(exportFunctionsPtr);
        
        unsafe
        {
            ManagedCallbacks* managedCallbacksPtr = (ManagedCallbacks*) unmanagedCallbacks;
            *managedCallbacksPtr = ManagedCallbacks.Create();
        }
    }
}