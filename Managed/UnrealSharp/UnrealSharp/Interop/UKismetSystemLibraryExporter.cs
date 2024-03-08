using System;
using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UKismetSystemLibraryExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, float, LinearColor, NativeBool, NativeBool,void> PrintString;
}