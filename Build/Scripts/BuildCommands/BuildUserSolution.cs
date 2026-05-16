using AutomationTool;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("BuildUserSolution", "Builds the UnrealSharp user solution (.sln) for the specified build configuration.")]
[Help("BuildConfig=<Config>", "The build configuration (Debug, DebugGame, Development, Shipping, etc.).")]
public class BuildUserSolution : BuildCommand
{
    public override void ExecuteBuild()
    {
        UnrealTargetConfiguration BuildConfig = ParseOptionalEnumParam<UnrealTargetConfiguration>("BuildConfig") ?? UnrealTargetConfiguration.Development;
        BuildCommands.BuildSolution.RunBuild(this.GetProjectScriptFolder(), BuildConfig, false);
    }
}