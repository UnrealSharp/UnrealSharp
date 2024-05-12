namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FArrayPropertyExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, int, void> InitializeArray;
    public static delegate* unmanaged<IntPtr, IntPtr, void> EmptyArray;
    public static delegate* unmanaged<IntPtr, IntPtr, void> AddToArray;
    public static delegate* unmanaged<IntPtr, IntPtr, int, void> InsertInArray;
    public static delegate* unmanaged<IntPtr, IntPtr, int, void> RemoveFromArray;
    public static delegate* unmanaged<IntPtr, IntPtr, int, void> ResizeArray;
    public static delegate* unmanaged<IntPtr, IntPtr, int, int, void> SwapValues;
}