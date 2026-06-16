using UnrealSharp.Binds;

namespace UnrealSharp.Core.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_TObjectPtr
{
    public static delegate* unmanaged<IntPtr, IntPtr, void> SetTObjectPtrPropertyValue;
}