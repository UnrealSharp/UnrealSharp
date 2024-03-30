namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class AsyncExporter
{
    public static delegate* unmanaged<int, IntPtr, void> RunOnThread;
    public static delegate* unmanaged<int> GetCurrentNamedThread;
}