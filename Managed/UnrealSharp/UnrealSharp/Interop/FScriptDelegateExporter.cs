namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FScriptDelegateExporter
{
    public static delegate* unmanaged<ref DelegateData, IntPtr, void> BroadcastDelegate;
}