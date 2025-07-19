using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FSubsystemCollectionBaseRefExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> InitializeDependency;
}
