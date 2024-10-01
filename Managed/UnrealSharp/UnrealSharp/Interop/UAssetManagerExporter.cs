using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class UAssetManagerExporter
{
    public static delegate* unmanaged<IntPtr> GetAssetManager;
}