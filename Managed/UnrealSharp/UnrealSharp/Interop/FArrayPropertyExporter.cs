namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FArrayPropertyExporter
{
    public static delegate* unmanaged<IntPtr, UnmanagedArray*, int, void> InitializeArray;
    public static delegate* unmanaged<IntPtr, UnmanagedArray*, void> EmptyArray;
    public static delegate* unmanaged<IntPtr, UnmanagedArray*, void> AddToArray;
    public static delegate* unmanaged<IntPtr, UnmanagedArray*, int, void> InsertInArray;
    public static delegate* unmanaged<IntPtr, UnmanagedArray*, int, void> RemoveFromArray;
    public static delegate* unmanaged<IntPtr, UnmanagedArray*, int, void> ResizeArray;
    public static delegate* unmanaged<IntPtr, UnmanagedArray*, int, int, void> SwapValues;
}