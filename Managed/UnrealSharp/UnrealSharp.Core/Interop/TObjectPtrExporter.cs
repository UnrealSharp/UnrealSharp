using UnrealSharp.Binds;

namespace UnrealSharp.Core.Interop;

[NativeCallbacks]
public static unsafe partial class TObjectPtrExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, void> SetTObjectPtrPropertyValue;
}