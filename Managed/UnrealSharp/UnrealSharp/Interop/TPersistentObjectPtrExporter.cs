using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class TPersistentObjectPtrExporter
{
    public static delegate* unmanaged<ref PersistentObjectPtrData, IntPtr> Get;
    public static delegate* unmanaged<ref PersistentObjectPtrData, IntPtr> GetNativePointer;
    public static delegate* unmanaged<ref PersistentObjectPtrData, IntPtr, void> FromObject;
}