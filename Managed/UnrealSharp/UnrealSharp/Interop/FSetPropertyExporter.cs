namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FSetPropertyExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr> GetElementProp;
}