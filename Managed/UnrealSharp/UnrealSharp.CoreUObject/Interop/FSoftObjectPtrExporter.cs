using UnrealSharp.Core.Attributes;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.CoreUObject.Interop;

[NativeCallbacks]
public static unsafe partial class FSoftObjectPtrExporter
{
    public static delegate* unmanaged<ref PersistentObjectPtrData, IntPtr> LoadSynchronous;
}