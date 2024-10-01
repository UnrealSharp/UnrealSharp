using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class UKismetSystemLibraryExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, float, FLinearColor, NativeBool, NativeBool,void> PrintString;
}