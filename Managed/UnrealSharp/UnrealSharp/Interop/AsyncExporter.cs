namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class AsyncExporter
{
    public static delegate* unmanaged<IntPtr, int, IntPtr, void> RunOnThread;
    public static delegate* unmanaged<int> GetCurrentNamedThread;
}