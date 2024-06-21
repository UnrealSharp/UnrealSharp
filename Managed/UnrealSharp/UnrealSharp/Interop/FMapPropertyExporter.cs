namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FMapPropertyExporter
{
    public static delegate* unmanaged<IntPtr, FScriptMapLayout> GetScriptLayout;
}