namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FScriptMapHelperExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, void> AddPair;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr> FindOrAdd;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, int> FindMapPairIndexFromHash;
    public static delegate* unmanaged<IntPtr, IntPtr, int> Num;
    public static delegate* unmanaged<IntPtr, IntPtr, void> EmptyValues;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, void> Remove;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, int> RemoveIndex;
    public static delegate* unmanaged<IntPtr, IntPtr, int, NativeBool> IsValidIndex;
    public static delegate* unmanaged<IntPtr, IntPtr, int> GetMaxIndex;
    public static delegate* unmanaged<IntPtr, IntPtr, int, IntPtr> GetPairPtr;
}