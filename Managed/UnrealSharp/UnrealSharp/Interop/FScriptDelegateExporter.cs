using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FScriptDelegateExporter
{
    public static delegate* unmanaged<IntPtr, FName, IntPtr, void> BroadcastDelegate;
    public static delegate* unmanaged<IntPtr, NativeBool> IsBound;
    
    public static delegate* unmanaged<IntPtr, IntPtr, FName, void> MakeDelegate;
    public static delegate* unmanaged<IntPtr, out IntPtr, out FName, void> GetDelegateInfo;
}