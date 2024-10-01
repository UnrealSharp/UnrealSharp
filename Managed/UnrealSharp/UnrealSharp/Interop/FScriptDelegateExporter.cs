using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class FScriptDelegateExporter
{
    public static delegate* unmanaged<ref DelegateData, IntPtr, void> BroadcastDelegate;
    public static delegate* unmanaged<IntPtr, NativeBool> IsBound;
}