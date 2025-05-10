using UnrealSharp.Binds;

namespace UnrealSharp.UnrealSharpAsync;

[NativeCallbacks]
public static unsafe partial class UCSAsyncBaseExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, void> InitializeAsyncObject;
}