using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UFunctionExporter
{
    public static delegate* unmanaged<IntPtr, UInt16> GetNativeFunctionParamsSize;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr> CreateNativeFunctionCustomStructSpecialization;
    public static delegate* unmanaged<IntPtr, IntPtr, void> InitializeFunctionParams;
}