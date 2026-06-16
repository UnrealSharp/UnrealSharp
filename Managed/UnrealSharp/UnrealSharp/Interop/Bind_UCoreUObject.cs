using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_UCoreUObject
{
    public static delegate* unmanaged<string, string?, string, IntPtr> GetType;
    public static delegate* unmanaged<string, string?, string, IntPtr> GetNativeDelegate;
    public static delegate* unmanaged<IntPtr, IntPtr> GetGeneratedClassFromSkeleton;
}