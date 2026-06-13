using UnrealSharp.Attributes;
using UnrealSharp.Editor.Interop;
using UnrealSharp.Engine.Core.Modules;

namespace UnrealSharp.Editor;

[UModule]
public class FUnrealSharpEditor : IModuleInterface
{
    public void StartupModule()
    {
        FManagedUnrealSharpEditorCallbacks callbacks = new FManagedUnrealSharpEditorCallbacks();
        FUnrealSharpEditorModuleExporter.CallInitializeUnrealSharpEditorCallbacks(callbacks);
    }

    public void ShutdownModule()
    {
    }
}