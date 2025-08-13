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
using UnrealSharp.Shared;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator;

[UnrealHeaderTool]
public static class Program
{
	public static IUhtExportFactory Factory { get; private set; } = null!;
	public static UHTManifest.Module PluginModule => Factory.PluginModule!;

    public static string EngineGluePath { get; private set; } = "";
    public static string PluginsPath { get; private set; } = "";
    public static string ProjectGluePath_LEGACY { get; private set; } = "";
    public static string ProjectName => Path.GetFileNameWithoutExtension(Factory.Session.ProjectFile!);

    public static bool BuildingEditor { get; private set; }
    public static UhtClass BlueprintFunctionLibrary { get; private set; } = null!;

    public static string PluginDirectory { get; private set; } = "";
    public static string ManagedBinariesPath { get; private set; } = "";
    public static string ManagedPath { get; private set; } = "";
    public static string ScriptFolder { get; private set; } = "";
    public static ImmutableArray<ProjectDirInfo> PluginDirs { get; private set; }

    [UhtExporter(Name = "UnrealSharpCore", Description = "Exports C++ to C# code", Options = UhtExporterOptions.Default,
	    ModuleName = "UnrealSharpCore")]
	private static void Main(IUhtExportFactory factory)
    {
        Console.WriteLine("Initializing C# Glue Generator...");
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

	        stopwatch.Stop();
	        Console.WriteLine($"Export process completed successfully in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");

	        if (CSharpExporter.HasModifiedEngineGlue && BuildingEditor)
	        {
	            Console.WriteLine("Detected modified engine glue. Building UnrealSharp solution...");
	            DotNetUtilities.BuildSolution(Path.Combine(ManagedPath, "UnrealSharp"), ManagedBinariesPath);
	        }

	        CreateGlueProjects();
	        CopyGlobalJson();
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
        ProjectGluePath_LEGACY = Path.Combine(ScriptFolder, "ProjectGlue");

		EngineGluePath = ScriptGeneratorUtilities.TryGetPluginDefine("GENERATED_GLUE_PATH");

		ManagedBinariesPath = Path.Combine(PluginDirectory, "Binaries", "Managed");

		ManagedPath = Path.Combine(PluginDirectory, "Managed");

		BuildingEditor = ScriptGeneratorUtilities.TryGetPluginDefine("BUILDING_EDITOR") == "1";

        DirectoryInfo pluginsDir = new DirectoryInfo(PluginsPath);

        PluginDirs = pluginsDir.GetFiles("*.uplugin", SearchOption.AllDirectories)
		        .Where(x => x.Directory!.GetDirectories("Source").Length != 0)
		        .Select(x => new ProjectDirInfo(Path.GetFileNameWithoutExtension(x.Name), x.DirectoryName!))
		        .Where(x => x.GlueProjectName != "UnrealSharp")
		        .ToImmutableArray();
	}
    
    private static void CreateGlueProjects()
    {
        foreach (KeyValuePair<UhtPackage, ProjectDirInfo> pluginInfo in PluginUtilities.PluginInfo)
        {
	        ProjectDirInfo pluginDir = pluginInfo.Value;
            TryCreateGlueProject(pluginDir.GlueCsProjPath, pluginDir.GlueProjectName, pluginDir.Dependencies, pluginDir.ProjectRoot);
        }
    }

    private static void TryCreateGlueProject(string csprojPath, string projectName, IEnumerable<string>? dependencyPaths, string projectRoot)
    {
        if (!File.Exists(csprojPath))
        {
	        string projectDirectory = Path.GetDirectoryName(csprojPath)!;
	        List<KeyValuePair<string, string>> arguments = new List<KeyValuePair<string, string>>
	        {
		        new("NewProjectName", projectName),
		        new("NewProjectFolder", Path.GetDirectoryName(projectDirectory)!),
		        new("SkipIncludeProjectGlue", "true"),
		        new("SkipSolutionGeneration", "true"),
	        };

	        arguments.Add(new KeyValuePair<string, string>("ProjectRoot", projectRoot));
	        if (!DotNetUtilities.InvokeUSharpBuildTool("GenerateProject", ManagedBinariesPath,
		            ProjectName,
		            PluginDirectory,
		            Factory.Session.ProjectDirectory!,
		            Factory.Session.EngineDirectory!,
		            arguments))
	        {
		        throw new InvalidOperationException($"Failed to create project file at {csprojPath}");
	        }
	        
	        Console.WriteLine($"Successfully created project file: {projectName}");
        }
        else
        {
	        Console.WriteLine($"Project file already exists: {projectName}. Skipping creation.");
        }
        
        AddPluginDependencies(projectName, csprojPath, dependencyPaths);
    }
    
    private static void CopyGlobalJson()
    {
	    string globalJsonPath = Path.Combine(ManagedPath, "global.json");
	    
	    if (!File.Exists(globalJsonPath))
	    {
		    throw new FileNotFoundException("global.json not found in Managed directory.", globalJsonPath);
	    }
		
	    string destinationPath = Path.Combine(Factory.Session.ProjectDirectory!, "global.json");
		
	    if (File.Exists(destinationPath))
	    {
		    File.Delete(destinationPath);
	    }
		
	    File.Copy(globalJsonPath, destinationPath);
    }

    private static void AddPluginDependencies(string projectName, string projectPath, IEnumerable<string>? dependencies)
    {
	    List<KeyValuePair<string, string>> arguments = new()
	    {
		    new KeyValuePair<string, string>("ProjectPath", projectPath),
	    };

	    if (dependencies != null)
	    {
		    foreach (string path in dependencies)
		    {
			    arguments.Add(new KeyValuePair<string, string>("Dependency", path));
		    }
	    }

	    if (!DotNetUtilities.InvokeUSharpBuildTool("UpdateProjectDependencies", ManagedBinariesPath,
		        ProjectName,
		        PluginDirectory,
		        Factory.Session.ProjectDirectory!,
		        Factory.Session.EngineDirectory!,
		        arguments))
	    {
		    throw new InvalidOperationException($"Failed to update project dependencies for {projectPath}");
	    }
	    
	    Console.WriteLine($"Updated project dependencies for {projectName}");
    }
}
