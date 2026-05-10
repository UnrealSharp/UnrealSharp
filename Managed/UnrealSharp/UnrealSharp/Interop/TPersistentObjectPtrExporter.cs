using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class TPersistentObjectPtrExporter
{
    public static delegate* unmanaged<ref FPersistentObjectPtrData<FSoftObjectPathUnsafe>, IntPtr> Get;
    public static delegate* unmanaged<ref FPersistentObjectPtrData<FSoftObjectPathUnsafe>, IntPtr> GetNativePointer;
    public static delegate* unmanaged<ref FPersistentObjectPtrData<FSoftObjectPathUnsafe>, IntPtr, void> FromObject;
    public static delegate* unmanaged<ref FPersistentObjectPtrData<FSoftObjectPathUnsafe>, IntPtr, void> FromSoftObjectPath;
    public static delegate* unmanaged<ref FPersistentObjectPtrData<FSoftObjectPathUnsafe>, IntPtr> GetUniqueID;
    
    public static delegate* unmanaged<ref FPersistentObjectPtrData<FSoftObjectPathUnsafe>, ref FPersistentObjectPtrData<FSoftObjectPathUnsafe>, NativeBool> Equals;
    public static delegate* unmanaged<ref FPersistentObjectPtrData<FSoftObjectPathUnsafe>, int> GetHashCode;
}