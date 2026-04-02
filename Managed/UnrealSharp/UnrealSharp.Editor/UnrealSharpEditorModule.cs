using UnrealSharp.Editor.Interop;
using UnrealSharp.Engine.Core.Modules;

namespace UnrealSharp.Editor;

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