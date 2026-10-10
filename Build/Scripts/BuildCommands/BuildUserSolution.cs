using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutomationTool;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Builds the user written C# code for the active project and emits the user load order.")]
[Help("OutputPath=<Path>", "Output path for the build output and emitted load order.")]
[Help("TargetConfiguration=<Config>", "The build configuration (Debug, DebugGame, Development, Shipping, etc.). Defaults to Development.")]
[Help("clp=<Args>", "Optional CLP arguments to pass to the build process.")]
[Help("ExtraArguments=<Arg>+<Arg>", "Additional arguments forwarded to dotnet build/publish.")]
public class BuildUserSolution : BuildCommand
{
    public override void ExecuteBuild()
    {
        ManagedBuildTarget Target = ManagedBuildTarget.FromCommand(this);
        List<FileInfo> Projects = this.GetManagedProjectFiles(Target)
            .Where(file => Target.Type == TargetType.Editor || !ProjectUtilities.IsEditorOnlyProject(file.FullName))
            .ToList();
        if (Projects.Count == 0)
        {
            LoggerUtilities.LogUnrealSharpInfo("No managed user projects found. Skipping user build.");
            return;
        }

        string SolutionDirectory = GenerateUserSolution.PrepareSolution(this, Target, Projects,
            GenerateUserSolution.GetBuildDirectory(this, Target), "UnrealSharpUser");
        List<KeyValuePair<string, string>> CommandParams = new List<KeyValuePair<string, string>>
        {
            new("TargetConfiguration", Target.Configuration.ToString()),
            new("TargetType", Target.Type.ToString()),
            new("TargetPlatform", Target.Platform.ToString()),
            new("ExtraArguments", $"-p:UETargetType={Target.Type}"),
            new("ExtraArguments", $"-p:UEBuildConfig={Target.Configuration}"),
            new("LoadOrderName", LoadOrderUtilities.UserLoadOrderName),
            new("SolutionDirectory", SolutionDirectory),
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

        foreach (FileInfo Project in Projects)
        {
            CommandParams.Add(new KeyValuePair<string, string>("Projects", Project.FullName));
        }

        if (!CommandUtilities.RunCommand(nameof(BuildEmitLoadOrder), this, CommandParams))
        {
            throw new AutomationException("Failed to build managed user projects.");
        }
    }
}
