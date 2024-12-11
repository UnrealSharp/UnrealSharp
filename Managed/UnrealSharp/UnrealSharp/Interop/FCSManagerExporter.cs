using UnrealSharp.Logging;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FCSManagerExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr> FindManagedObject;
    public static delegate* unmanaged<IntPtr> GetCurrentWorldContext;
    public static delegate* unmanaged<IntPtr> GetCurrentWorldPtr;
}