using UnrealSharp.Binds;
using UnrealSharp.Core;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UKismetSystemLibraryExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, float, FLinearColor, NativeBool, NativeBool,void> PrintString;
}