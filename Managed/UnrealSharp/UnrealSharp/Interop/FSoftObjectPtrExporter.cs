using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class FSoftObjectPtrExporter
{
    public static delegate* unmanaged<ref PersistentObjectPtrData, IntPtr> LoadSynchronous;
}