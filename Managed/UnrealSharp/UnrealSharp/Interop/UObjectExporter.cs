namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UObjectExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr> CreateNewObject;
    public static delegate* unmanaged<IntPtr> GetTransientPackage;
    public static delegate* unmanaged<IntPtr, out FName, void> NativeGetName;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, void> InvokeNativeFunction;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, void> InvokeNativeStaticFunction;
    public static delegate* unmanaged<IntPtr, bool> NativeIsValid;
    public static delegate* unmanaged<IntPtr, IntPtr> GetWorld_Internal;
    public static delegate* unmanaged<IntPtr, int> GetUniqueID;
}