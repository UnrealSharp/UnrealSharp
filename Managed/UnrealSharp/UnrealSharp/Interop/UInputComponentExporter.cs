using UnrealSharp.SlateCore;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UInputComponentExporter
{
    public static delegate* unmanaged<IntPtr, Name, InputEvent, IntPtr, Name, NativeBool, NativeBool, void> BindAction;
    public static delegate* unmanaged<IntPtr, Name, InputEvent, IntPtr, Name, NativeBool, NativeBool, void> BindActionKeySignature;
    public static delegate* unmanaged<IntPtr, Name, IntPtr, Name, NativeBool, NativeBool, void> BindAxis;
}