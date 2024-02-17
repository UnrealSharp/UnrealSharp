namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FSoftObjectPtrExporter
{
    public static delegate* unmanaged<ref PersistentObjectPtrData, IntPtr> LoadSynchronous;
}