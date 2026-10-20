using AutomationTool;
using EpicGames.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;
using UnrealSharp.Shared;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Builds the auto-generated UnrealSharp glue projects for the active project and emits the glue load order.")]
[Help("TargetConfiguration=<Config>", "The build configuration (Debug, DebugGame, Development, Shipping, etc.).")]
[Help("TargetType=<Type>", "The target type (Editor, Game, etc.) to build glue for.")]
[Help("OutputDirectory=<OutputDirectory>", "The directory to output the built glue assemblies to.")]
[Help("AddReferences=<true|false>", "Whether to add references to the user project.")]
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
        bool AddReferences = ParseParamBool("AddReferences", false);
        string[] ExtraArguments = ParseParamValues("ExtraArguments");

        Build(this, TargetType, TargetConfiguration, OutputDirectory, AddReferences, ExtraArguments);
    }

    public static void Build(BuildCommand command, TargetType targetType, UnrealTargetConfiguration buildConfig, string outputDirectory, bool addReferences, IList<string>? extraArguments = null)
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

        if (addReferences)
        {
            AddUserProjectReferences(command);
        }
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

    private static void AddUserProjectReferences(BuildCommand command)
    {
        string GlueFileName = AssemblyUtilities.MakeLoadOrderFileName(LoadOrderUtilities.GlueLoadOrderName);
        string GlueSource = PathUtilities.BuildOutputPath(command.GetProjectRootFolder());
        string GlueManifest = Path.Combine(GlueSource, GlueFileName);

        if (!File.Exists(GlueManifest))
        {
            LoggerUtilities.LogUnrealSharpWarning($"Runtime glue manifest not found at {GlueManifest}.");
            return;
        }

        IEnumerable<string> Dependencies = AssemblyUtilities.ReadLoadOrder(GlueManifest)
            .Select(name => Path.Combine(GlueSource, name + ".dll"))
            .Where(File.Exists);

        IEnumerable<FileInfo> ManagedProjects = command.GetManagedProjectFiles()
            .Where(file => !file.Name.Contains("RuntimeGlue", StringComparison.OrdinalIgnoreCase));

        foreach (FileInfo Project in ManagedProjects)
        {
            DirectoryInfo? ProjectDirectory = Project.Directory;
            if (ProjectDirectory is null)
            {
                LoggerUtilities.LogUnrealSharpWarning($"Skipping adding references for {Project.FullName}: parent directory is null.");
                continue;
            }

            LoggerUtilities.LogUnrealSharpInfo($"Adding project references for {Project.Name}.");

            XmlDocument CsprojDocument = new XmlDocument();
            CsprojDocument.Load(Project.FullName);
            CsprojDocument.EnsureProjectRoot();

            XmlElement ItemGroup = CsProjectUtilities.GetOrCreateItemGroup(CsprojDocument);
            CsProjectUtilities.AddProjectReferences(CsprojDocument, ItemGroup, ProjectDirectory.FullName, Dependencies);

            CsprojDocument.Save(Project.FullName);
        }
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
