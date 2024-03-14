using UnrealSharp.Attributes;
using UnrealSharp.Engine;
using UnrealSharp.Plugins;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UInputComponentExporter
{
    public static delegate* unmanaged<IntPtr, Name, EInputEvent, IntPtr, Name, NativeBool, NativeBool, void> BindAction;
    public static delegate* unmanaged<IntPtr, Name, IntPtr, Name, NativeBool, NativeBool, void> BindAxis;
}