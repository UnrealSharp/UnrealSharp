using UnrealSharp.Binds;
using UnrealSharp.Core;
using UnrealSharp.EnhancedInput;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_UEnhancedInputComponent
{
    public static delegate* unmanaged<IntPtr, IntPtr, ETriggerEvent, IntPtr, FName, IntPtr, bool> BindAction;
    public static delegate* unmanaged<IntPtr, uint, bool> RemoveBindingByHandle;
}