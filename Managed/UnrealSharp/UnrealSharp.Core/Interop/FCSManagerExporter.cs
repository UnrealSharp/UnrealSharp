using UnrealSharp.Core.Attributes;

namespace UnrealSharp.Core.Interop;

[NativeCallbacks]
public static unsafe partial class FCSManagerExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr> FindManagedObject;
}