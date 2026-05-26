using System.Collections.Generic;
using System.IO;
using AutomationTool;
using EpicGames.Core;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("BuildConfig=<Config>", "The build configuration (Debug, DebugGame, Development, Shipping, etc.).")]
[Help("TargetType=<Type>", "The target type (Editor, Game, etc.) to build glue for.")]
[Help("OutputDirectory=<OutputDirectory>", "The directory to output the built glue assemblies to.")]
public class BuildUserGlue : BuildCommand
{
    public override void ExecuteBuild()
    {
        if (this.IsInstalledUnrealSharpBuild())
        {
            throw new AutomationException("BuildUserGlue should not be executed in an installed UnrealSharp build.");
        }
        
        TargetType TargetType = ParseRequiredEnumParamEnum<TargetType>("TargetType");
        UnrealTargetConfiguration BuildConfig = ParseRequiredEnumParamEnum<UnrealTargetConfiguration>("BuildConfig");
        string OutputDirectory = ParseRequiredStringParam("OutputDirectory");
        Build(this, TargetType, BuildConfig, OutputDirectory);
    }

    public static void Build(BuildCommand command, TargetType buildConfig, UnrealTargetConfiguration targetConfiguration, string outputDirectory)
    {
        string SolutionDirectory = Path.Combine(command.GetUnrealSharpIntermediateDirectory(), "Temp");
        List<string> GlueProjectPaths = GetGlueProjectPaths(command, buildConfig);
        
        GenerateSolution(command, SolutionDirectory, GlueProjectPaths);
        BuildSolution(command, SolutionDirectory, outputDirectory, targetConfiguration, GlueProjectPaths);
    }

    private static void GenerateSolution(BuildCommand command, string solutionPath, List<string> glueProjectPaths)
    {
        LoggerUtilities.LogUnrealSharpInfo($"Generating UnrealSharp user solution at {solutionPath}...");
        
        List<KeyValuePair<string, string>> CommandParams = new List<KeyValuePair<string, string>>
        {
            new("SolutionName", "UnrealSharpGlue"),
            new("OutputFolder", solutionPath),
        };
        
        foreach (string GlueProjectPath in glueProjectPaths)
        {
            CommandParams.Add(new KeyValuePair<string, string>("ProjectPaths", GlueProjectPath));
        }
        
        CommandUtilities.RunCommand("GenerateSolution", command, CommandParams);
    }
    
    private static void BuildSolution(BuildCommand buildCommand, string solutionOutputDirectory, string publishDirectory, UnrealTargetConfiguration buildConfig, List<string> glueProjectPaths)
    {
        LoggerUtilities.LogUnrealSharpInfo($"Building UnrealSharp glue projects in {solutionOutputDirectory} with build configuration {buildConfig}...");
        
        if (!Directory.Exists(publishDirectory))
        {
            Directory.CreateDirectory(publishDirectory);
        }
        
        List<KeyValuePair<string, string>> actionArgs = new List<KeyValuePair<string, string>>
        {
            new("SolutionDirectory", solutionOutputDirectory),
            new("BuildConfig", buildConfig.ToString()),
            new("OutputPath", publishDirectory),
            new("LoadOrderName", LoadOrderUtilities.GlueLoadOrderName),
            new("IsCollectible", "false"),
            new("Priority", LoadOrderUtilities.GlueLoadOrderPriority.ToString()),
        };
        
        foreach (string GlueProjectPath in glueProjectPaths)
        {
            actionArgs.Add(new KeyValuePair<string, string>("Projects", GlueProjectPath));
        }
        
        CommandUtilities.RunCommand("BuildEmitLoadOrder", buildCommand, actionArgs);
    }

    private static List<string> GetGlueProjectPaths(BuildCommand command, TargetType targetType)
    {
        IEnumerable<FileReference> GameProjects = command.GetGameModules();
        
        List<string> GlueProjectPaths = new List<string>();
        
        foreach (FileReference Project in GameProjects)
        {
            string UhtOutputFolder = PathUtilities.GetUhtGeneratedOutputPath(Project.Directory.FullName, targetType);
            
            if (!Directory.Exists(UhtOutputFolder))
            {
                continue;
            }
            
            GlueProjectPaths.AddRange(Directory.GetFiles(UhtOutputFolder, "*.csproj", SearchOption.AllDirectories));
        }
        
        return GlueProjectPaths;
    }
}