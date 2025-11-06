using UnrealSharp.Binds;

#if UE_5_5_OR_LATER
using UnrealSharp.CoreUObject;
#else
using UnrealSharp.StructUtils;
#endif

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FInstancedStructExporter
{
    private static readonly delegate* unmanaged<ref FInstancedStructData, IntPtr> GetNativeStruct;
    private static readonly delegate* unmanaged<ref FInstancedStructData, void> NativeInit;
    private static readonly delegate* unmanaged<ref FInstancedStructData, ref FInstancedStructData, void> NativeCopy;
    private static readonly delegate* unmanaged<ref FInstancedStructData, void> NativeDestroy;
    private static readonly delegate* unmanaged<ref FInstancedStructData, IntPtr, IntPtr, void> InitializeAs;
    private static readonly delegate* unmanaged<ref FInstancedStructData, IntPtr> GetMemory;
}