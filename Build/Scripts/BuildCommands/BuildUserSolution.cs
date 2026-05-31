using System.Collections.Generic;
using System.IO;
using AutomationTool;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Builds the user-authored C# code for the active project and emits the user load order.")]
[Help("OutputPath=<Path>", "Output path for the build output and emitted load order.")]
[Help("BuildConfig=<Config>", "The build configuration (Debug, DebugGame, Development, Shipping, etc.). Defaults to Development.")]
[Help("clp=<Args>", "Optional CLP arguments to pass to the build process.")]
[Help("ExtraArguments=<Arg>+<Arg>", "Additional arguments forwarded to dotnet build/publish.")]
public class BuildUserSolution : BuildCommand
{
    public override void ExecuteBuild()
    {
        List<KeyValuePair<string, string>> CommandParams = new List<KeyValuePair<string, string>>
        {
            new("BuildConfig", ParseParamValue("BuildConfig", nameof(UnrealTargetConfiguration.Development))),
            new("LoadOrderName", LoadOrderUtilities.UserLoadOrderName),
            new("SolutionDirectory", this.GetProjectScriptFolder()),
            new("OutputPath", ParseRequiredStringParam("OutputPath")),
            new("IsCollectible", "true"),
            new("Priority", LoadOrderUtilities.UserLoadOrderPriority.ToString())
        };

        foreach (string ClpValue in ParseParamValues("clp"))
        {
            if (string.IsNullOrWhiteSpace(ClpValue))
            {
                continue;
            }

            CommandParams.Add(new KeyValuePair<string, string>("clp", ClpValue));
        }

        foreach (string ExtraArgument in ParseParamValues("ExtraArguments"))
        {
            if (string.IsNullOrWhiteSpace(ExtraArgument))
            {
                continue;
            }

            CommandParams.Add(new KeyValuePair<string, string>("ExtraArguments", ExtraArgument));
        }

        foreach (FileInfo Project in this.GetManagedProjectFiles())
        {
            CommandParams.Add(new KeyValuePair<string, string>("Projects", Project.FullName));
        }

        CommandUtilities.RunCommand(nameof(BuildEmitLoadOrder), this, CommandParams);
    }
}