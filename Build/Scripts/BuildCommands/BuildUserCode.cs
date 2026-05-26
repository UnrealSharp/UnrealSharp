using System.Collections.Generic;
using System.IO;
using AutomationTool;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("OutputPath=<Path>", "Optional output path for the build output.")]
[Help("BuildConfig=<Config>", "The build configuration (Debug, DebugGame, Development, Shipping, etc.).")]
public class BuildUserCode : BuildCommand
{
    public override void ExecuteBuild()
    {
        List<KeyValuePair<string, string>> CommandParams = new List<KeyValuePair<string, string>>
        {
            new("BuildConfig", "Development"),
            new("LoadOrderName", LoadOrderUtilities.UserLoadOrderName),
            new("SolutionDirectory", this.GetProjectScriptFolder()),
            new("OutputPath", ParseRequiredStringParam("OutputPath")),
            new("IsCollectible", "true"),
            new("Priority", LoadOrderUtilities.UserLoadOrderPriority.ToString()),
            new("clp", ParseParamValue("clp") ?? string.Empty)
        };

        List<FileInfo> Projects = this.GetManagedProjectFiles();
        foreach (FileInfo Project in Projects)
        {
            CommandParams.Add(new KeyValuePair<string, string>("Projects", Project.FullName));
        }
        
        CommandUtilities.RunCommand(nameof(BuildEmitLoadOrder), this, CommandParams);
    }
}