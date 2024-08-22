namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FScriptSetExporter
{
    public static delegate* unmanaged<ref ScriptSet, int, NativeBool> IsValidIndex;
    public static delegate* unmanaged<ref ScriptSet, int> Num;
    public static delegate* unmanaged<ref ScriptSet, int> GetMaxIndex;
    public static delegate* unmanaged<int, ref ScriptSet, IntPtr, IntPtr> GetData;
    public static delegate* unmanaged<int, ref ScriptSet, ref FScriptSetLayout, void> Empty;
    public static delegate* unmanaged<int, ref ScriptSet, ref FScriptSetLayout, void> RemoveAt;
    public static delegate* unmanaged<ref ScriptSet, ref FScriptSetLayout, int> AddUninitialized;
    public static delegate* unmanaged<int, int, FScriptSetLayout> GetScriptSetLayout;
}