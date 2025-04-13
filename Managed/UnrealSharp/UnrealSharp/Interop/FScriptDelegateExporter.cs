﻿using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FScriptDelegateExporter
{
    public static delegate* unmanaged<ref DelegateData, IntPtr, void> BroadcastDelegate;
    public static delegate* unmanaged<IntPtr, NativeBool> IsBound;
}