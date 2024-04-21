namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UStructExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, void> InitializeStruct;
}