using System.Collections.Generic;
using AutomationTool;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Packages the UnrealSharp managed code for an installed build")] 
[Help("UEBuildConfig=<Config>", "Optional. The build configuration (Debug, Development, Shipping, Test).")]
[Help("UETargetType=<Type>", "Optional. The target type (Editor, Game, etc.). Defaults to Editor.")]
[Help("TargetPlatform=<Platform>", "Optional. Target platform. Defaults to Win64.")]
[Help("TargetArchitecture=<Arch>", "Optional. Target architecture. Defaults to X64.")]
public class StageUnrealSharp : BuildCommand
{
    public override void ExecuteBuild()
    {
        List<KeyValuePair<string, string>> ActionArgs =
        [
            new("ArchiveDirectory", this.GetProjectRootFolder()),
            new("UEBuildConfig", ParseParamValue("UEBuildConfig", nameof(UnrealTargetConfiguration.Development))),
            new("UETargetType", ParseParamValue("UETargetType", nameof(TargetType.Editor))),
            new("TargetPlatform", ParseParamValue("TargetPlatform", string.Empty)),
            new("TargetArchitecture", ParseParamValue("TargetArchitecture", string.Empty))
        ];
        
        CommandUtilities.RunCommand(nameof(PackageProject), this, ActionArgs);
    }
}