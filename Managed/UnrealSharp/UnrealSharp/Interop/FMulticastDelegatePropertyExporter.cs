using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FMulticastDelegatePropertyExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, string, void> AddDelegate;
    public static delegate* unmanaged<IntPtr, NativeBool> IsBound;
    public static delegate* unmanaged<IntPtr, ref UnmanagedArray, void> ToString;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, string, void> RemoveDelegate;
    public static delegate* unmanaged<IntPtr, IntPtr, void> ClearDelegate;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, void> BroadcastDelegate;
    public static delegate* unmanaged<IntPtr, IntPtr> GetSignatureFunction;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, string, NativeBool> ContainsDelegate; 
}