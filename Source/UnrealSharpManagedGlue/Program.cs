using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
	
	public static string EngineGluePath = "";
	public static string ProjectGluePath = "";

	public static bool BuildingEditor;
	public static UhtClass BlueprintFunctionLibrary = null!;
	
	public static string PluginDirectory = "";
	public static string ManagedBinariesPath = "";
	public static string ManagedPath = "";
	public static string ScriptFolder = "";
	
	[UhtExporter(Name = "UnrealSharpCore", Description = "Exports C++ to C# code", Options = UhtExporterOptions.Default,
	    ModuleName = "UnrealSharpCore",
	    CppFilters = new[] { "*.generated.cs" }, HeaderFilters = new[] { "*.generated.cs" },
	    OtherFilters = new[] { "*.generated.cs" })]
	private static void Main(IUhtExportFactory factory)
    {
        Console.WriteLine("Initializing UnrealSharpScriptGenerator...");
	    Factory = factory;
	    
	    InitializeStatics();

	    UhtType? foundType = factory.Session.FindType(null, UhtFindOptions.SourceName | UhtFindOptions.Class, "UBlueprintFunctionLibrary");
	    if (foundType is not UhtClass blueprintFunctionLibrary)
	    {
	        throw new Exception("Failed to find UBlueprintFunctionLibrary class.");
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
	        
	        TryCreateGlueProject();
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
	
	static void InitializeStatics()
	{
		PluginDirectory = ScriptGeneratorUtilities.TryGetPluginDefine("PLUGIN_PATH");
		
		DirectoryInfo unrealSharpDirectory = Directory.GetParent(PluginDirectory)!.Parent!;
		ScriptFolder = Path.Combine(unrealSharpDirectory.FullName, "Script");
		
		EngineGluePath = ScriptGeneratorUtilities.TryGetPluginDefine("GENERATED_GLUE_PATH");
		ProjectGluePath = Path.Combine(ScriptFolder, "ProjectGlue");
		
		ManagedBinariesPath = Path.Combine(PluginDirectory, "Binaries", "Managed");
		
		ManagedPath = Path.Combine(PluginDirectory, "Managed");
		
		BuildingEditor = ScriptGeneratorUtilities.TryGetPluginDefine("BUILDING_EDITOR") == "1";
	}

	static void TryCreateGlueProject()
	{
		string csprojPath = Path.Combine(ProjectGluePath, "ProjectGlue.csproj");

		if (File.Exists(csprojPath))
		{
			return;
		}

		Dictionary<string, string> arguments = new Dictionary<string, string>
		{
			{ "NewProjectName", "ProjectGlue" },
			{ "NewProjectPath", $"\"{ProjectGluePath}\""},
			{ "SkipIncludeProjectGlue", "true" }
		};

		string engineDirectory = Factory.Session.EngineDirectory!;
		string projectDirectory = Factory.Session.ProjectDirectory!;
		string projectName = Path.GetFileNameWithoutExtension(Program.Factory.Session.ProjectFile)!;
		
		
		UnrealSharp.Shared.DotNetUtilities.InvokeUSharpBuildTool("GenerateProject", ManagedBinariesPath, 
			projectName, 
			PluginDirectory, 
			projectDirectory, 
			engineDirectory, 
			arguments);
	}
}