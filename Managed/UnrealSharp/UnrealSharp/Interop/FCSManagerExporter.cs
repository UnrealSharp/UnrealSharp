using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class FCSManagerExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr> FindManagedObject;
}