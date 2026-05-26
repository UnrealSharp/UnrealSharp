using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutomationTool;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;
using UnrealSharp.Shared;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Builds the solution and emits a JSON file with the load order of the assemblies.")]
[Help("SolutionDirectory=<Path>", "Path to the solution to build.")]
[Help("LoadOrderName=<Name>", "Name for the emitted load order file, without extension.")]
[Help("OutputPath=<Path>", "Optional output path for the build output.")]
[Help("BuildConfig=<Config>", "The build configuration (Debug, DebugGame, Development, Shipping, etc.).")]
[Help("clp=<Args>", "Optional CLP arguments to pass to the build process.")]
[Help("IsCollectible=<true/false>", "Whether the emitted assemblies should be marked as collectible. Defaults to true.")]
[Help("Priority=<Number>", "Optional priority for the emitted load order. Higher priority load orders will be loaded first.")]
[Help("Projects=<Path>+<Path>", "Optional list of project files to include in the load order. If not specified, all projects in the solution will be included.")]
public class BuildEmitLoadOrder : BuildCommand
{
    private const string GlueProjectSuffix = ".Glue";

    public override void ExecuteBuild()
    {
        string OutputPath = ParseRequiredStringParam("OutputPath");
        List<string> ExtraArguments = new List<string>
        {
            $"-p:PublishDir={OutputPath}"
        };

        string[] Clp = ParseParamValues("clp");
        if (Clp.Length > 0)
        {
            ExtraArguments.Add($"-clp:{string.Join(';', Clp)}");
        }
        
        UnrealTargetConfiguration BuildConfig = ParseRequiredEnumParamEnum<UnrealTargetConfiguration>("BuildConfig");
        string SolutionPath = ParseRequiredStringParam("SolutionDirectory");
        
        BuildCommands.BuildSolution.RunBuild(SolutionPath, BuildConfig, publish: true, ExtraArguments);
        
        string LoadOrderName = ParseRequiredStringParam("LoadOrderName");
        IEnumerable<string> Projects = ParseParamValues("Projects");
        
        LoadOrderOptions Options = new LoadOrderOptions
        {
            Collectible = ParseParamBool("IsCollectible"),
            Priority = ParseParamInt("Priority")
        };

        EmitLoadOrder(Projects.ToList(), LoadOrderName, OutputPath, Options);
        AddLaunchSettings(this);
    }

    public static void EmitLoadOrder(List<string> projectFiles, string loadOrderName, string outputPath, LoadOrderOptions options)
    {
        LoadOrderUtilities.TryEmitLoadOrder(projectFiles, outputPath, loadOrderName, options);
    }

    private static void AddLaunchSettings(BuildCommand buildCommand)
    {
        ArgumentNullException.ThrowIfNull(buildCommand);

        List<FileInfo> AllProjectFiles = buildCommand.GetManagedProjectFiles();

        foreach (FileInfo ProjectFile in AllProjectFiles)
        {
            DirectoryInfo? ProjectDirectory = ProjectFile.Directory;
            if (ProjectDirectory is null)
            {
                LoggerUtilities.LogUnrealSharpWarning($"Skipping launch-settings for {ProjectFile.FullName}: parent directory is null.");
                continue;
            }

            if (ProjectDirectory.Name.EndsWith(GlueProjectSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            LaunchSettingsScaffolding.EnsureProjectLaunchSettings(buildCommand, ProjectDirectory.Name);
        }
    }
}