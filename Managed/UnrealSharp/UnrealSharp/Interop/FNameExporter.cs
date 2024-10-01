using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class FNameExporter
{
    public static delegate* unmanaged<FName, ref UnmanagedArray, void> NameToString;
    public static delegate* unmanaged<ref FName, IntPtr, void> StringToName;
    public static delegate* unmanaged<FName, bool> IsValid;
}