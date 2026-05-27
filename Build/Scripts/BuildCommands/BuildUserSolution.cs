using AutomationTool;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Builds the UnrealSharp user solution (.sln) for the specified build configuration.")]
[Help("BuildConfig=<Config>", "The build configuration (Debug, DebugGame, Development, Shipping, etc.). Defaults to Development.")]
[Help("Publish", "If set, the user solution is published instead of built.")]
[Help("ExtraArguments=<Arg>+<Arg>", "Additional arguments forwarded to dotnet build/publish.")]
public class BuildUserSolution : BuildCommand
{
    public override void ExecuteBuild()
    {
        UnrealTargetConfiguration BuildConfig = ParseOptionalEnumParam<UnrealTargetConfiguration>("BuildConfig") ?? UnrealTargetConfiguration.Development;
        bool Publish = ParseParam("Publish");
        string[] ExtraArguments = ParseParamValues("ExtraArguments");

        BuildCommands.BuildSolution.RunBuild(this.GetProjectScriptFolder(), BuildConfig, Publish, ExtraArguments);
    }
}