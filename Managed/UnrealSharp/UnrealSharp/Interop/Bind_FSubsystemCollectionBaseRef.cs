using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FSubsystemCollectionBaseRef
{
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> InitializeDependency;
}
