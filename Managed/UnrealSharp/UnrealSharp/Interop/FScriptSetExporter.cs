namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FScriptSetExporter
{
    public static delegate* unmanaged<ref FScriptSet, int, NativeBool> IsValidIndex;
    public static delegate* unmanaged<ref FScriptSet, int> Num;
    public static delegate* unmanaged<ref FScriptSet, int> GetMaxIndex;
    public static delegate* unmanaged<int, ref FScriptSet, IntPtr, IntPtr> GetData;
    public static delegate* unmanaged<int, ref FScriptSet, ref FScriptSetLayout, void> Empty;
    public static delegate* unmanaged<int, ref FScriptSet, ref FScriptSetLayout, void> RemoveAt;
    public static delegate* unmanaged<ref FScriptSet, ref FScriptSetLayout, int> AddUninitialized;
    public static delegate* unmanaged<ref FScriptSet, ref FScriptSetLayout, IntPtr, HashDelegates.GetKeyHash, HashDelegates.Equality, int> FindIndex;
    public static delegate* unmanaged<ref FScriptSet, ref FScriptSetLayout, IntPtr, HashDelegates.GetKeyHash, HashDelegates.Equality, HashDelegates.Construct, HashDelegates.Destruct, void> Add;
    public static delegate* unmanaged<ref FScriptSet, ref FScriptSetLayout, IntPtr, HashDelegates.GetKeyHash, HashDelegates.Equality, HashDelegates.Construct, int> FindOrAdd;
}