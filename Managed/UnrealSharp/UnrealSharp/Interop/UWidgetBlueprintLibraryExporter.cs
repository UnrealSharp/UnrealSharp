using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class UWidgetBlueprintLibraryExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr> CreateWidget;
}