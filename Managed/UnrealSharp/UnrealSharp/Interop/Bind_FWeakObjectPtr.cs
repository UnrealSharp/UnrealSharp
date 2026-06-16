using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FWeakObjectPtr
{
    public static delegate* unmanaged<WeakObjectData, IntPtr> GetObject;
    public static delegate* unmanaged<ref WeakObjectData, IntPtr, void> SetObject;
    public static delegate* unmanaged<WeakObjectData, NativeBool> IsValid;
    public static delegate* unmanaged<WeakObjectData, NativeBool> IsStale;
    public static delegate* unmanaged<WeakObjectData, WeakObjectData, NativeBool> NativeEquals;
}