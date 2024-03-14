using UnrealSharp.Attributes;
using UnrealSharp;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public unsafe partial class FScriptDelegateExporter
{
    public static delegate* unmanaged<ref DelegateData, IntPtr, void> BroadcastDelegate;
}