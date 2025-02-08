using UnrealSharp.Editor.HotReload;
using UnrealSharp.Engine;
using UnrealSharp.Engine.Core.Modules;

namespace UnrealSharp.Editor;

public class FUnrealSharpEditor : IModuleInterface
{
    private HotReloadManager hotReloadManager;
    
    public FUnrealSharpEditor()
    {
        hotReloadManager = new HotReloadManager();
    }
    
    public void StartupModule()
    {
        
    }

    public void ShutdownModule()
    {

    }
}
