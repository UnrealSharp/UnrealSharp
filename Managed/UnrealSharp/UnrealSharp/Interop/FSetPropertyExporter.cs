namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FSetPropertyExporter
{
    public static delegate* unmanaged<IntPtr, FScriptSetLayout> GetScriptSetLayout;
    public static delegate* unmanaged<IntPtr, IntPtr> GetElementProp;
}