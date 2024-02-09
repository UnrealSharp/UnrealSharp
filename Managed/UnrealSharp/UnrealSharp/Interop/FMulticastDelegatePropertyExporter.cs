namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FMulticastDelegatePropertyExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, string, void> AddDelegate;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, string, void> RemoveDelegate;
    public static delegate* unmanaged<IntPtr, IntPtr, void> ClearDelegate;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, void> BroadcastDelegate;
    public static delegate* unmanaged<IntPtr, IntPtr> GetSignatureFunction;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, string, NativeBool> ContainsDelegate; 
}