using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using AutomationTool;
using EpicGames.Core;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Builds the auto-generated UnrealSharp glue projects for the active project and emits the glue load order.")]
[Help("TargetConfiguration=<Config>", "The build configuration (Debug, DebugGame, Development, Shipping, etc.).")]
[Help("TargetType=<Type>", "The target type (Editor, Game, etc.) to build glue for.")]
[Help("OutputDirectory=<OutputDirectory>", "The directory to output the built glue assemblies to.")]
[Help("ExtraArguments=<Arg>+<Arg>", "Additional arguments forwarded to dotnet build/publish.")]
public class BuildUserGlue : BuildCommand
{
    public override void ExecuteBuild()
    {
        if (this.IsInstalledUnrealSharpBuild())
        {
            throw new AutomationException("BuildUserGlue should not be executed in an installed UnrealSharp build.");
        }

        TargetType TargetType = ParseRequiredEnumParamEnum<TargetType>("TargetType");
        UnrealTargetConfiguration TargetConfiguration = ParseRequiredEnumParamEnum<UnrealTargetConfiguration>("TargetConfiguration");
        string OutputDirectory = ParseRequiredStringParam("OutputDirectory");
        string[] ExtraArguments = ParseParamValues("ExtraArguments");

        Build(this, TargetType, TargetConfiguration, OutputDirectory, ExtraArguments);
    }

    public static void Build(BuildCommand command, TargetType targetType, UnrealTargetConfiguration buildConfig, string outputDirectory, IList<string>? extraArguments = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(outputDirectory);

        string SolutionDirectory = Path.Combine(command.GetUnrealSharpIntermediateDirectory(), "Build", targetType.ToString());
        List<string> GlueProjectPaths = GetGlueProjectPaths(command, targetType);

        if (GlueProjectPaths.Count == 0)
        {
            LoggerUtilities.LogUnrealSharpInfo("No glue projects found. Skipping glue build.");
            return;
        }

        GenerateSolution(command, SolutionDirectory, GlueProjectPaths);
        BuildSolution(command, SolutionDirectory, outputDirectory, buildConfig, GlueProjectPaths, extraArguments);
        CreateSolutionStamp(SolutionDirectory, GlueProjectPaths);
    }

    private static void GenerateSolution(BuildCommand command, string solutionDirectory, List<string> glueProjectPaths)
    {
        if (IsSolutionStampSame(solutionDirectory, glueProjectPaths))
        {
            return;
        }
        
        const string solutionName = "UnrealSharpGlue";
        
        List<KeyValuePair<string, string>> CommandParams = new List<KeyValuePair<string, string>>
        {
            new("SolutionName", solutionName),
            new("OutputFolder", solutionDirectory),
        };

        foreach (string GlueProjectPath in glueProjectPaths)
        {
            CommandParams.Add(new KeyValuePair<string, string>("ProjectPaths", GlueProjectPath));
        }
        
        LoggerUtilities.LogUnrealSharpInfo($"Generating UnrealSharp user solution at {solutionDirectory}...");
        CommandUtilities.RunCommand(nameof(BuildCommands.GenerateSolution), command, CommandParams);
    }
    
    private static string GetStampPath(string solutionDirectory) => Path.Combine(solutionDirectory, "UnrealSharpSolutionStamp.json");

    private static bool IsSolutionStampSame(string solutionDirectory, List<string> glueProjectPaths)
    {
        string SolutionStampPath = GetStampPath(solutionDirectory);

        if (!File.Exists(SolutionStampPath))
        {
            return false;
        }

        string SolutionStampContent = File.ReadAllText(SolutionStampPath);
        JsonArray? GlueProjectsArray = JsonSerializer.Deserialize<JsonArray>(SolutionStampContent);

        if (GlueProjectsArray == null)
        {
            return false;
        }

        return glueProjectPaths.ToList().SequenceEqual(glueProjectPaths);
    }

    private static void CreateSolutionStamp(string solutionPath, List<string> glueProjectPaths)
    {
        JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = false,
        };
        
        JsonArray GlueProjectArray = new JsonArray();
        foreach (string GlueProjectPath in glueProjectPaths)
        {
            GlueProjectArray.Add(GlueProjectPath);
        }
        
        string JsonString = JsonSerializer.Serialize(GlueProjectArray, Options);
        File.WriteAllText(GetStampPath(solutionPath), JsonString);
        
        LoggerUtilities.LogUnrealSharpInfo("Creating Solution Stamp...");
    }
        
        
    private static void BuildSolution(BuildCommand buildCommand, string solutionOutputDirectory, string publishDirectory, UnrealTargetConfiguration buildConfig, List<string> glueProjectPaths, IList<string>? extraArguments)
    {
        LoggerUtilities.LogUnrealSharpInfo($"Building UnrealSharp glue projects in {solutionOutputDirectory} with build configuration {buildConfig}...");

        if (!Directory.Exists(publishDirectory))
        {
            Directory.CreateDirectory(publishDirectory);
        }

        List<KeyValuePair<string, string>> ActionArgs = new List<KeyValuePair<string, string>>
        {
            new("SolutionDirectory", solutionOutputDirectory),
            new("TargetConfiguration", buildConfig.ToString()),
            new("OutputPath", publishDirectory),
            new("LoadOrderName", LoadOrderUtilities.GlueLoadOrderName),
            new("IsCollectible", "false"),
            new("Priority", LoadOrderUtilities.GlueLoadOrderPriority.ToString()),
        };

        foreach (string GlueProjectPath in glueProjectPaths)
        {
            ActionArgs.Add(new KeyValuePair<string, string>("Projects", GlueProjectPath));
        }

        if (extraArguments != null)
        {
            foreach (string ExtraArgument in extraArguments)
            {
                if (string.IsNullOrWhiteSpace(ExtraArgument))
                {
                    continue;
                }

                ActionArgs.Add(new KeyValuePair<string, string>("ExtraArguments", ExtraArgument));
            }
        }

        CommandUtilities.RunCommand(nameof(BuildEmitLoadOrder), buildCommand, ActionArgs);
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