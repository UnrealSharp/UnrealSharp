﻿using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UCoreUObjectExporter
{
    public static delegate* unmanaged<string, string?, string, IntPtr> GetType;
}