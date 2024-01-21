namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FNameExporter
{
    public static delegate* unmanaged<Name, ref UnmanagedArray, void> NameToString;
    public static delegate* unmanaged<ref Name, IntPtr, void> StringToName;
    public static delegate* unmanaged<Name, bool> IsValid;
}