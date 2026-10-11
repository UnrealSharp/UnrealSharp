using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        ManagedBuildTarget Target = ManagedBuildTarget.FromCommand(this);
        PrepareSolution(this, Target, this.GetManagedProjectFiles(Target),
            OutputFolder, SolutionName, ForceGenerate);
    }

    internal static string GetBuildDirectory(BuildCommand command, ManagedBuildTarget target)
    {
        return Path.Combine(command.GetUnrealSharpIntermediateDirectory(), "Build", "User",
            target.Platform.ToString(), target.Type.ToString(), target.Configuration.ToString());
    }

    internal static string PrepareSolution(BuildCommand command, ManagedBuildTarget target, IEnumerable<FileInfo> projects,
        string outputDirectory, string solutionName, bool forceGenerate = false)
    {
        string SolutionPath = Path.Combine(outputDirectory, solutionName + ".sln");
        string StampPath = SolutionPath + ".projects.json";
        string[] ProjectPaths = projects.Select(project => project.FullName).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        string Stamp = JsonSerializer.Serialize(new { Platform = target.Platform.ToString(), target.Type, target.Configuration, ProjectPaths });

        if (!forceGenerate && File.Exists(SolutionPath) && File.Exists(StampPath) && File.ReadAllText(StampPath) == Stamp)
        {
            return outputDirectory;
        }

        // Explicit paths keep solution generation and publishing on the same project set.
        List<KeyValuePair<string, string>> Arguments = new()
        {
            new("SolutionName", solutionName),
            new("OutputFolder", outputDirectory)
        };
        Arguments.AddRange(ProjectPaths.Select(path => new KeyValuePair<string, string>("ProjectPaths", path)));
        if (!CommandUtilities.RunCommand(nameof(GenerateSolution), command, Arguments))
        {
            throw new AutomationException($"Failed to generate managed solution '{SolutionPath}'.");
        }

        File.WriteAllText(StampPath, Stamp);
        return outputDirectory;
    }
}
