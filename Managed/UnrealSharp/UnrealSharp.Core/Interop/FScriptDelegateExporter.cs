using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public unsafe partial class FScriptDelegateExporter
{
    public static delegate* unmanaged<ref DelegateData, IntPtr, void> BroadcastDelegate;
}