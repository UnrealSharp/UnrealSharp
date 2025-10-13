using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FNameExporter
{
    public static delegate* unmanaged<FName, ref UnmanagedArray, void> NameToString;
    public static delegate* unmanaged<ref FName, char*, int, void> StringToName;
    public static delegate* unmanaged<FName, NativeBool> IsValid;
}