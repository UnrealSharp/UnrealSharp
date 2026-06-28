using UnrealSharp.Core;
using UnrealSharp.Core.Interop;
using UnrealSharp.Engine;
using UnrealSharp.Interop;

namespace UnrealSharp.UnrealSharpCore;

public readonly partial record struct FSubsystemCollectionBaseRef
{
    private readonly IntPtr _collectionRef;

    public T? InitializeDependency<T>(TSubclassOf<T> subsystemClass) where T : USubsystem
    {
        IntPtr obj = Bind_FSubsystemCollectionBaseRef.CallInitializeDependency(_collectionRef, subsystemClass.NativeClass);
        IntPtr handle = Bind_UCSManager.CallFindManagedObject(obj);
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }

    public T? InitializeDependency<T>() where T : USubsystem
    {
        return InitializeDependency(new TSubclassOf<T>(typeof(T)));
    }

    public T InitializeRequiredSubsystem<T>(TSubclassOf<T> subsystemClass) where T : USubsystem
    {
        return InitializeDependency(subsystemClass) ?? throw new InvalidOperationException($"Subsystem {typeof(T).Name} is not initialized.");
    }

    public T InitializeRequiredSubsystem<T>() where T : USubsystem
    {
        return InitializeRequiredSubsystem(new TSubclassOf<T>(typeof(T)));
    }
}
