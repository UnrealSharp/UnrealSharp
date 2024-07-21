using System;
using System.Diagnostics;
using System.IO;
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
	

	[UhtExporter(Name = "CSharpForUE", Description = "Exports C++ to C# code", Options = UhtExporterOptions.Default,
		ModuleName = "CSharpForUE",
		CppFilters = new[] { "*.generated.cs" }, HeaderFilters = new[] { "*.generated.cs" },
		OtherFilters = new[] { "*.generated.cs" })]
	private static void Main(IUhtExportFactory factory)
	{
		Console.WriteLine("Initializing C# exporter...");
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
			
			// Clean up generated c# glue from .h/.cpp files that have been removed.
			FileExporter.CleanOldExportedFiles();
			
			// Everything is exported. Now we need to compile the generated C# code, if necessary.
			if (FileExporter.HasModifiedEngineGlue && BuildingEditor)
			{
				string engineGluePath = Path.Combine(ManagedPath, "UnrealSharp");
				DotNetUtilities.BuildSolution(engineGluePath);
			}
        
			stopwatch.Stop();
			Console.WriteLine($"Export process completed successfully in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
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
		ProjectGluePath = Path.Combine(ScriptFolder, "obj", "generated");
		
		ManagedBinariesPath = Path.Combine(PluginDirectory, "Binaries", "Managed");
		
		ManagedPath = Path.Combine(PluginDirectory, "Managed");
		
		BuildingEditor = ScriptGeneratorUtilities.TryGetPluginDefine("BUILDING_EDITOR") == "1";
	}
}