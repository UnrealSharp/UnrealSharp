using UnrealSharp.Attributes;
using UnrealSharp.EnhancedInput;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class UEnhancedInputComponentExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, ETriggerEvent, IntPtr, FName, void> BindAction;
}