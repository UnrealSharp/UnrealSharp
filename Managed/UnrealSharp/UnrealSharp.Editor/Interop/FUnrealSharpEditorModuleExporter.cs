using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Editor.Interop;

[NativeCallbacks]
public static unsafe partial class FUnrealSharpEditorModuleExporter
{
    public static delegate* unmanaged<FManagedUnrealSharpEditorCallbacks, void> InitializeUnrealSharpEditorCallbacks;
    public static delegate* unmanaged<out UnmanagedArray, void> GetProjectPaths;
    public static delegate* unmanaged<string, string, string, void> DirtyUnrealType;
}