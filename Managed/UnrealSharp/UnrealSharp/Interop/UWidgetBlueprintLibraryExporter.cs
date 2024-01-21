namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class UWidgetBlueprintLibraryExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr> CreateWidget;
}