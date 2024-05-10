using UnrealSharp.Engine;
using UnrealSharp.EnhancedInput;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UInputComponentExporter
{
    public static delegate* unmanaged<IntPtr, Name, EInputEvent, IntPtr, Name, NativeBool, NativeBool, void> BindAction;
    public static delegate* unmanaged<IntPtr, Name, EInputEvent, IntPtr, Name, NativeBool, NativeBool, void> BindActionKeySignature;
    public static delegate* unmanaged<IntPtr, Name, IntPtr, Name, NativeBool, NativeBool, void> BindAxis;
}