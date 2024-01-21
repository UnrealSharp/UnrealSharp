namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class TPersistentObjectPtrExporter
{
    public static delegate* unmanaged<ref PersistentObjectPtrData, IntPtr> Get;
    public static delegate* unmanaged<ref PersistentObjectPtrData, IntPtr> GetNativePointer;
    public static delegate* unmanaged<ref PersistentObjectPtrData, IntPtr, void> FromObject;
}