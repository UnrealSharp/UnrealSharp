﻿using UnrealSharp.Binds;
using UnrealSharp.EnhancedInput;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UEnhancedInputComponentExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, ETriggerEvent, IntPtr, FName, IntPtr, bool> BindAction;
    public static delegate* unmanaged<IntPtr, uint, bool> RemoveBindingByHandle;
}