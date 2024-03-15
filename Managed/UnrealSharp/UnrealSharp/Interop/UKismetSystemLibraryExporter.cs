using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;
using UnrealSharp.Plugins;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UKismetSystemLibraryExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, float, LinearColor, NativeBool, NativeBool,void> PrintString;
}