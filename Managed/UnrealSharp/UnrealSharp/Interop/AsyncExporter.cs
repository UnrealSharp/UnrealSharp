using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class AsyncExporter
{
    public static delegate* unmanaged<int, IntPtr, void> RunOnThread;
    public static delegate* unmanaged<int> GetCurrentNamedThread;
}