using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FScriptDelegate
{
    public static delegate* unmanaged<IntPtr, FName, IntPtr, void> BroadcastDelegate;
    public static delegate* unmanaged<IntPtr, NativeBool> IsBound;
    
    public static delegate* unmanaged<IntPtr, IntPtr, FName, void> MakeDelegate;
    public static delegate* unmanaged<IntPtr, out IntPtr, out FName, void> GetDelegateInfo;
    public static delegate* unmanaged<IntPtr, IntPtr, void> ProcessDelegate;
    public static delegate* unmanaged<IntPtr, IntPtr, FName, NativeBool> ContainsDelegate;
    public static delegate* unmanaged<IntPtr, void> ClearDelegate;
    public static delegate* unmanaged<IntPtr, IntPtr, void> CopyDelegate;
}
