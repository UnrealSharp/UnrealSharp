using UnrealSharp.Core;
using UnrealSharp.Engine;
using UnrealSharp.Interop;

namespace UnrealSharp.UnrealSharpCore;

public readonly ref partial struct FSubsystemCollectionBaseRef
{
    private readonly IntPtr _collectionRef;

    public T? InitializeDependency<T>(TSubclassOf<T> subsystemClass) where T : USubsystem
    {
        IntPtr obj = FSubsystemCollectionBaseRefExporter.CallInitializeDependency(_collectionRef, subsystemClass.NativeClass);
        IntPtr handle = FCSManagerExporter.CallFindManagedObject(obj);
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }

    public T? InitializeDependency<T>() where T : USubsystem
    {
        return InitializeDependency(new TSubclassOf<T>(typeof(T)));
    }

    public T InitializeRequiredSubsystem<T>(TSubclassOf<T> subsystemClass) where T : USubsystem
    {
        return InitializeDependency<T>(subsystemClass) ?? throw new InvalidOperationException($"Subsystem {typeof(T).Name} is not initialized.");
    }

    public T InitializeRequiredSubsystem<T>() where T : USubsystem
    {
        return InitializeRequiredSubsystem(new TSubclassOf<T>(typeof(T)));
    }
}
