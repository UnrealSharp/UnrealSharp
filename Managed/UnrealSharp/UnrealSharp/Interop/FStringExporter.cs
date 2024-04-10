namespace UnrealSharp.Interop;

[NativeCallbacks]
public unsafe partial class FStringExporter
{
    public static delegate* unmanaged<IntPtr, char*, void> MarshalToNativeString;
    public static delegate* unmanaged<ref UnmanagedArray, void> DisposeString;
}