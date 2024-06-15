namespace UnrealSharp.Interop;

[NativeCallbacks] 
public static unsafe partial class FPropertyExporter
{
    public static delegate* unmanaged<IntPtr, string, IntPtr> GetNativePropertyFromName;
    public static delegate* unmanaged<IntPtr, int> GetPropertyOffset;
    public static delegate* unmanaged<IntPtr, int> GetSize;
    public static delegate* unmanaged<IntPtr, int> GetArrayDim;
    public static delegate* unmanaged<IntPtr, IntPtr, void> DestroyValue;
    public static delegate* unmanaged<IntPtr, IntPtr, void> InitializeValue;
    public static delegate* unmanaged<IntPtr, string, int> GetPropertyOffsetFromName;
    public static delegate* unmanaged<IntPtr, string, int> GetPropertyArrayDimFromName;
}