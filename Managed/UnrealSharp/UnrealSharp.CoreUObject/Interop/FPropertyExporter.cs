using UnrealSharp.Core.Attributes;

namespace UnrealSharp.CoreUObject.Interop;

[NativeCallbacks] 
public static unsafe partial class FPropertyExporter
{
    public static delegate* unmanaged<IntPtr, string, IntPtr> GetNativePropertyFromName;
    public static delegate* unmanaged<IntPtr, int> GetPropertyOffset;
    public static delegate* unmanaged<IntPtr, string, int> GetPropertyOffsetFromName;
    public static delegate* unmanaged<IntPtr, string, int> GetPropertyArrayDimFromName;
}