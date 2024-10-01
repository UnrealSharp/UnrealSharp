using UnrealSharp.Attributes;
using UnrealSharp.Engine;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class UInputComponentExporter
{
    public static delegate* unmanaged<IntPtr, FName, EInputEvent, IntPtr, FName, NativeBool, NativeBool, void> BindAction;
    public static delegate* unmanaged<IntPtr, FName, EInputEvent, IntPtr, FName, NativeBool, NativeBool, void> BindActionKeySignature;
    public static delegate* unmanaged<IntPtr, FName, IntPtr, FName, NativeBool, NativeBool, void> BindAxis;
}