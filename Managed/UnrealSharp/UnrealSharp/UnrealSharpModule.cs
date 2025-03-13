using UnrealSharp.Engine.Core.Modules;
using UnrealSharp.Interop;

namespace UnrealSharp;

public class UnrealSharpModule : IModuleInterface
{
    public void StartupModule()
    {
        InitializeManagedCallbacks();
    }

    public void ShutdownModule()
    {
       
    }
    
    void InitializeManagedCallbacks()
    {
        IntPtr managedCallbacks = FCSManagedCallbacksExporter.CallGetManagedCallbacks();
        ManagedCallbacks.Create(managedCallbacks);
    }
}