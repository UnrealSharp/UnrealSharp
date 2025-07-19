using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Tables;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator;

[UnrealHeaderTool]
public static class Program
{
	public static IUhtExportFactory Factory { get; private set; } = null!;
	public static UHTManifest.Module PluginModule => Factory.PluginModule!;

    public static string EngineGluePath { get; private set; } = "";
    public static string ProjectGluePath { get; private set; } = "";
    public static string PluginsPath { get; private set; } = "";

    public static bool BuildingEditor { get; private set; }
    public static UhtClass BlueprintFunctionLibrary { get; private set; } = null!;

    public static string PluginDirectory { get; private set; } = "";
    public static string ManagedBinariesPath { get; private set; } = "";
    public static string ManagedPath { get; private set; } = "";
    public static string ScriptFolder { get; private set; } = "";
    public static ImmutableArray<PluginDirInfo> PluginDirs { get; private set; }

    [UhtExporter(Name = "UnrealSharpCore", Description = "Exports C++ to C# code", Options = UhtExporterOptions.Default,
	    ModuleName = "UnrealSharpCore",
	    CppFilters = ["*.generated.cs"], HeaderFilters = ["*.generated.cs"],
	    OtherFilters = ["*.generated.cs"])]
	private static void Main(IUhtExportFactory factory)
    {
        Console.WriteLine("Initializing UnrealSharpScriptGenerator...");
	    Factory = factory;

	    InitializeStatics();

	    UhtType? foundType = factory.Session.FindType(null, UhtFindOptions.SourceName | UhtFindOptions.Class, "UBlueprintFunctionLibrary");
	    if (foundType is not UhtClass blueprintFunctionLibrary)
	    {
	        throw new InvalidOperationException("Failed to find UBlueprintFunctionLibrary class.");
	    }

	    BlueprintFunctionLibrary = blueprintFunctionLibrary;

	    try
	    {
	        Stopwatch stopwatch = new Stopwatch();
	        stopwatch.Start();

	        CSharpExporter.StartExport();

	        FileExporter.CleanOldExportedFiles();

	        Console.WriteLine($"Export process completed successfully in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
	        stopwatch.Stop();

	        if (CSharpExporter.HasModifiedEngineGlue && BuildingEditor)
	        {
	            Console.WriteLine("Detected modified engine glue. Building UnrealSharp solution...");
	            UnrealSharp.Shared.DotNetUtilities.BuildSolution(Path.Combine(ManagedPath, "UnrealSharp"), ManagedBinariesPath);
	        }

	        TryCreateGlueProjects();
	    }
	    catch (Exception ex)
	    {
	        Console.ForegroundColor = ConsoleColor.Red;
	        Console.WriteLine("An error occurred during the export process:");
	        Console.WriteLine($"Error Message: {ex.Message}");
	        Console.WriteLine("Stack Trace:");
	        Console.WriteLine(ex.StackTrace);
	        Console.ResetColor();
	    }
	}

    private static void InitializeStatics()
	{
		PluginDirectory = ScriptGeneratorUtilities.TryGetPluginDefine("PLUGIN_PATH");

		DirectoryInfo unrealSharpDirectory = Directory.GetParent(PluginDirectory)!.Parent!;
		ScriptFolder = Path.Combine(unrealSharpDirectory.FullName, "Script");
        PluginsPath = Path.Combine(unrealSharpDirectory.FullName, "Plugins");

		EngineGluePath = ScriptGeneratorUtilities.TryGetPluginDefine("GENERATED_GLUE_PATH");
		ProjectGluePath = Path.Combine(ScriptFolder, "ProjectGlue");

		ManagedBinariesPath = Path.Combine(PluginDirectory, "Binaries", "Managed");

		ManagedPath = Path.Combine(PluginDirectory, "Managed");

		BuildingEditor = ScriptGeneratorUtilities.TryGetPluginDefine("BUILDING_EDITOR") == "1";

        var pluginsDir = new DirectoryInfo(PluginsPath);
        PluginDirs = [
            ..pluginsDir.GetFiles("*.uplugin", SearchOption.AllDirectories)
                .Select(x => new PluginDirInfo(Path.GetFileNameWithoutExtension(x.Name), x.DirectoryName!))
                .Where(x => x.PluginName != "UnrealSharp")
        ];
	}

    private static void TryCreateGlueProjects()
    {
        var projectGluePath = Path.Combine(ProjectGluePath, "ProjectGlue.csproj");
        var projectName = Path.GetFileNameWithoutExtension(Factory.Session.ProjectFile)!;
        TryCreateGlueProject(projectGluePath, Factory.Session.ProjectDirectory!,  projectName);
        foreach (var pluginDir in PluginDirs)
        {
            TryCreateGlueProject(pluginDir.GlueProjectPath, pluginDir.PluginDirectory, pluginDir.PluginName);
        }

        AddPluginDependencies(projectGluePath, Factory.Session.ProjectDirectory!, projectName,
                PluginUtilities.GetProjectDependencyPaths());
        foreach (var pluginDir in PluginDirs)
        {
            AddPluginDependencies(pluginDir.GlueProjectPath, pluginDir.PluginDirectory, pluginDir.PluginName,
                    PluginUtilities.GetPluginDependencyPaths(pluginDir.PluginName));
        }

        UpdateSolutionProjects();
    }

    private static void TryCreateGlueProject(string csprojPath, string projectDirectory,
                                             string projectName)
    {
        if (File.Exists(csprojPath))
        {
            return;
        }

        Dictionary<string, string> arguments = new Dictionary<string, string>
        {
            { "NewProjectName", Path.GetFileNameWithoutExtension(csprojPath) },
            { "SkipIncludeProjectGlue", "true" }
        };

        string engineDirectory = Factory.Session.EngineDirectory!;


        UnrealSharp.Shared.DotNetUtilities.InvokeUSharpBuildTool("GenerateProject", ManagedBinariesPath,
            projectName,
            PluginDirectory,
            projectDirectory,
            engineDirectory,
            arguments);
    }

    private static void AddPluginDependencies(string projectPath, string projectDirectory,
                                              string projectName, IEnumerable<string> projectPaths)
    {
        string engineDirectory = Factory.Session.EngineDirectory!;

        var arguments = Enumerable.Repeat(new KeyValuePair<string, string>("ProjectPath", projectPath), 1)
                .Concat(projectPaths.Select(x => new KeyValuePair<string, string>("Dependency", x)));

        UnrealSharp.Shared.DotNetUtilities.InvokeUSharpBuildTool("UpdateProjectDependencies", ManagedBinariesPath,
                projectName,
                PluginDirectory,
                projectDirectory,
                engineDirectory,
                arguments);
    }

    private static void UpdateSolutionProjects()
    {
        var projectDirectory = Factory.Session.ProjectDirectory!;
        var projectName = Path.GetFileNameWithoutExtension(Factory.Session.ProjectFile)!;
        string engineDirectory = Factory.Session.EngineDirectory!;

        var arguments = PluginDirs.Select(x => new KeyValuePair<string, string>("PluginPath", x.PluginScriptDir));

        UnrealSharp.Shared.DotNetUtilities.InvokeUSharpBuildTool("UpdateProjectSolution", ManagedBinariesPath,
                projectName,
                PluginDirectory,
                projectDirectory,
                engineDirectory,
                arguments);
    }
}
