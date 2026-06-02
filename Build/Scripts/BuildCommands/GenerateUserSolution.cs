using System.Collections.Generic;
using System.IO;
using AutomationTool;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Generates a user solution in the intermediate folder. This solution is used for features like Go To Definition to work in Unreal Engine source code.")]
[Help("ForceGenerate", "Whether to force generation of the user solution even if it already exists.")]
public class GenerateUserSolution : BuildCommand
{
    public override void ExecuteBuild()
    {
        bool ForceGenerate = ParseParam("ForceGenerate");
        
        string SolutionName = "Managed" + this.GetProjectName();
        string OutputFolder = this.GetProjectScriptFolder();
        string SolutionPath = Path.Combine(OutputFolder, SolutionName + ".sln");
        
        if (!ForceGenerate && File.Exists(SolutionPath))
        {
            return;
        }
        
        List<KeyValuePair<string, string>> ActionArgs =
        [
            new("SolutionName", SolutionName),
            new("OutputFolder", OutputFolder),
            new("SearchFolders", this.GetScriptDirectoryName())
        ];

        CommandUtilities.RunCommand(nameof(GenerateSolution), this, ActionArgs);
    }
}