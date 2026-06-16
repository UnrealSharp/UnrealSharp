using UnrealSharp.Binds;

namespace UnrealSharp.UnrealSharpAsync;

[NativeCallbacks]
public static unsafe partial class Bind_UCSAsyncBase
{
    public static delegate* unmanaged<IntPtr, IntPtr, void> InitializeAsyncObject;
}