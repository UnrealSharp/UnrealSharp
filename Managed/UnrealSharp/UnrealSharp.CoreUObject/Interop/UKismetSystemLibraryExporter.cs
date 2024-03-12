using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;

namespace UnrealSharp.CoreUObject.Interop;

public struct LinearColor
{
    public float R;
    public float G;
    public float B;
    public float A;
}

[NativeCallbacks]
public static unsafe partial class UKismetSystemLibraryExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, float, LinearColor, NativeBool, NativeBool,void> PrintString;
}