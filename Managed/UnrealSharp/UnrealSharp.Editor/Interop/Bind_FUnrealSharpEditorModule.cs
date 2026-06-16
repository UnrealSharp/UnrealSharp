using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Editor.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_FUnrealSharpEditorModule
{
    public static delegate* unmanaged<FManagedUnrealSharpEditorCallbacks, void> InitializeUnrealSharpEditorCallbacks;
    public static delegate* unmanaged<out UnmanagedArray, void> GetProjectPaths;
    public static delegate* unmanaged<string, string, string, ECSTypeStructuralFlags, void> DirtyUnrealType;
}