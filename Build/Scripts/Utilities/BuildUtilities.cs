using System.IO;
using AutomationTool;
using UnrealBuildBase;

namespace UnrealSharp.Automation.Utilities;

public static class BuildUtilities
{
    public const string UnrealSharpBuildFlagFileName = "UnrealSharpBuild.flag";
    
    public static bool IsInstalledUnrealSharpBuild(this BuildCommand buildCommand)
    {
        string InstalledUnrealSharpBuildPath = Path.Combine(buildCommand.GetProjectRootFolder(), "Binaries", "UnrealSharp", "InstalledUnrealSharp.flag");
        return File.Exists(InstalledUnrealSharpBuildPath) && Unreal.IsProjectInstalled();
    }
}