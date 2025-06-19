using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UObjectExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr> CreateNewObject;
    public static delegate* unmanaged<IntPtr> GetTransientPackage;
    public static delegate* unmanaged<IntPtr, out FName, void> NativeGetName;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, void> InvokeNativeFunction;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, void> InvokeNativeStaticFunction;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, void> InvokeNativeFunctionOutParms;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, void> InvokeNativeNetFunction;
    public static delegate* unmanaged<IntPtr, NativeBool> NativeIsValid;
    public static delegate* unmanaged<IntPtr, IntPtr> GetWorld_Internal;
    public static delegate* unmanaged<IntPtr, int> GetUniqueID;
}