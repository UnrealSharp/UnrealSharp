using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class TStrongObjectPtrExporter
{
    public static delegate* unmanaged<ref FStrongObjectPtr, IntPtr, void> ConstructStrongObjectPtr;
    public static delegate* unmanaged<ref FStrongObjectPtr, void> DestroyStrongObjectPtr;
}