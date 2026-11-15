using UnrealSharp.Binds;
using UnrealSharp.EnhancedInput;
using UnrealSharp.Core;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FSoftObjectPtr
{
    public static delegate* unmanaged<ref FPersistentObjectPtrData<FSoftObjectPathUnsafe>, IntPtr> LoadSynchronous;
    public static delegate* unmanaged<ref WeakObjectData, ref FTopLevelAssetPath, byte*, int, IntPtr> Get;
    public static delegate* unmanaged<IntPtr, int, IntPtr> ReadSoft;
    public static delegate* unmanaged<IntPtr, int, IntPtr> WriteSoft;
    public static delegate* unmanaged<IntPtr, ref FTopLevelAssetPath, ref UnmanagedArray, void> FromObject;
    public static delegate* unmanaged<ref WeakObjectData, ref FTopLevelAssetPath, byte*, int, byte, IntPtr> Resolve;
    public static delegate* unmanaged<ref FTopLevelAssetPath, byte*, int, int> PathHash;
}
