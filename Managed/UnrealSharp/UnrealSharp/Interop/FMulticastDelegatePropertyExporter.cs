namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FMulticastDelegatePropertyExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, string, void> AddDelegate;
    public static delegate* unmanaged<IntPtr, IntPtr, string, void> RemoveDelegate;
    public static delegate* unmanaged<IntPtr, void> ClearDelegate;
    public static delegate* unmanaged<IntPtr, IntPtr, void> BroadcastDelegate;
    public static delegate* unmanaged<IntPtr, IntPtr> GetSignatureFunction;
}