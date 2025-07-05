using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UFunctionExporter
{
    public static delegate* unmanaged<IntPtr, UInt16> GetNativeFunctionParamsSize;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr> CreateNativeFunctionCustomStructSpecialization;
    public static delegate* unmanaged<IntPtr, IntPtr, void> InitializeFunctionParams;
    public static delegate* unmanaged<IntPtr, NativeBool> HasBlueprintEventBeenImplemented;
    
    public static bool IsFunctionImplemented(IntPtr function)
    {
        return HasBlueprintEventBeenImplemented(function).ToManagedBool();
    }
}