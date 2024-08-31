namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FNameExporter
{
    public static delegate* unmanaged<FName, ref UnmanagedArray, void> NameToString;
    public static delegate* unmanaged<ref FName, IntPtr, void> StringToName;
    public static delegate* unmanaged<FName, bool> IsValid;
}