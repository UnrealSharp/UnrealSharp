using UnrealSharp.Attributes;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class FTextExporter
{
    public static delegate* unmanaged<ref FTextData, char*> ToString;
    public static delegate* unmanaged<ref FTextData, string, void> FromString;
    public static delegate* unmanaged<ref FTextData, FName, void> FromName;
    public static delegate* unmanaged<ref FTextData, void> CreateEmptyText;
}