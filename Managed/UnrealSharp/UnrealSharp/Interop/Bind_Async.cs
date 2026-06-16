using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_Async
{
    public static delegate* unmanaged<WeakObjectData, int, IntPtr, void> RunOnThread;
    public static delegate* unmanaged<int> GetCurrentNamedThread;
}