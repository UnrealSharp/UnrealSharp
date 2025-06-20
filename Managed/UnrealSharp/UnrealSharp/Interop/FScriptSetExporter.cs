using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FScriptSetExporter
{
    public static delegate* unmanaged<IntPtr, int, NativeBool> IsValidIndex;
    public static delegate* unmanaged<IntPtr, int> Num;
    public static delegate* unmanaged<IntPtr, int> GetMaxIndex;
    public static delegate* unmanaged<int, IntPtr, IntPtr, IntPtr> GetData;
    public static delegate* unmanaged<int, IntPtr, IntPtr, void> Empty;
    public static delegate* unmanaged<int, IntPtr, IntPtr, void> RemoveAt;
    public static delegate* unmanaged<IntPtr, IntPtr, int> AddUninitialized;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, HashDelegates.GetKeyHash, HashDelegates.Equality, int> FindIndex;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, HashDelegates.GetKeyHash, HashDelegates.Equality, HashDelegates.Construct, HashDelegates.Destruct, void> Add;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, HashDelegates.GetKeyHash, HashDelegates.Equality, HashDelegates.Construct, int> FindOrAdd;
}
