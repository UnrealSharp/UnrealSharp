namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UAssetManagerExporter
{
    public static delegate* unmanaged<IntPtr> GetAssetManager;
}