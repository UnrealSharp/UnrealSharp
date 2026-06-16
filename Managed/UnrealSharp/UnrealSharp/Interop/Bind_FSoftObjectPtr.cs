using UnrealSharp.Binds;
using UnrealSharp.EnhancedInput;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FSoftObjectPtr
{
    public static delegate* unmanaged<ref FPersistentObjectPtrData<FSoftObjectPathUnsafe>, IntPtr> LoadSynchronous;
}