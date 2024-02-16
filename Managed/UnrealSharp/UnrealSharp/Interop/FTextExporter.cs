namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FTextExporter
{
    public static delegate* unmanaged<ref TextData, char*> ToString;
    public static delegate* unmanaged<ref TextData, string, void> FromString;
    public static delegate* unmanaged<ref TextData, Name, void> FromName;
    public static delegate* unmanaged<ref TextData, void> CreateEmptyText;
}