using UnrealSharp.Binds;

namespace UnrealSharp.Core;

[NativeCallbacks]
public static unsafe partial class FCSManagerExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr> FindManagedObject;
    public static delegate* unmanaged<IntPtr> GetCurrentWorldContext;
    public static delegate* unmanaged<IntPtr> GetCurrentWorldPtr;
}