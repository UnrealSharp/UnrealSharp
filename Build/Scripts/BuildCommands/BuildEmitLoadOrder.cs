using System;
using System.Collections.Generic;
using System.IO;
using AutomationTool;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;
using UnrealSharp.Shared;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Builds the solution and emits a JSON file with the load order of the assemblies.")]
[Help("SolutionDirectory=<Path>", "Path to the solution to build.")]
[Help("LoadOrderName=<Name>", "Name for the emitted load order file, without extension.")]
[Help("OutputPath=<Path>", "Output path for the build output and emitted load order.")]
[Help("TargetConfiguration=<Config>", "The build configuration (Debug, DebugGame, Development, Shipping, etc.).")]
[Help("clp=<Args>", "Optional CLP arguments to pass to the build process.")]
[Help("ExtraArguments=<Arg>+<Arg>", "Additional arguments forwarded to dotnet build/publish.")]
[Help("IsCollectible=<true/false>", "Whether the emitted assemblies should be marked as collectible. Defaults to false when omitted.")]
[Help("Priority=<Number>", "Optional priority for the emitted load order. Higher priority load orders will be loaded first.")]
[Help("Projects=<Path>+<Path>", "Optional list of project files to include in the load order. If not specified, all projects in the solution will be included.")]
public class BuildEmitLoadOrder : BuildCommand
{
    private const string GlueProjectSuffix = ".Glue";

    public override void ExecuteBuild()
    {
        string SolutionPath = ParseRequiredStringParam("SolutionDirectory");
        UnrealTargetConfiguration TargetConfiguration = ParseRequiredEnumParamEnum<UnrealTargetConfiguration>("TargetConfiguration");
        string OutputPath = ParseRequiredStringParam("OutputPath");
        string LoadOrderName = ParseRequiredStringParam("LoadOrderName");
        string[] Projects = ParseParamValues("Projects");

        LoadOrderOptions Options = new LoadOrderOptions
        {
            Collectible = ParseParamBool("IsCollectible"),
            Priority = ParseParamInt("Priority")
        };

        List<string> BuildArguments = BuildSolutionArguments(OutputPath);

        BuildCommands.BuildSolution.RunBuild(SolutionPath, TargetConfiguration, publish: true, BuildArguments);
        
        EmitLoadOrder(Projects, LoadOrderName, OutputPath, Options);
        AddLaunchSettings(this);
    }
    
    public static void EmitLoadOrder(IEnumerable<string> projectFiles, string loadOrderName, string outputPath, LoadOrderOptions options)
    {
        LoadOrderUtilities.TryEmitLoadOrder(projectFiles, outputPath, loadOrderName, options);
    }

    private List<string> BuildSolutionArguments(string outputPath)
    {
        List<string> Arguments = new List<string>
        {
            $"-p:PublishDir={outputPath}"
        };

        string[] Clp = ParseParamValues("clp");
        if (Clp.Length > 0)
        {
            Arguments.Add($"-clp:{string.Join(';', Clp)}");
        }

        string[] ForwardedArguments = ParseParamValues("ExtraArguments");
        if (ForwardedArguments.Length > 0)
        {
            Arguments.AddRange(ForwardedArguments);
        }

        return Arguments;
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