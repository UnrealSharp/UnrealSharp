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

	        Console.WriteLine("Exporting C++ to C#...");
	        CSharpExporter.StartExport();

	        Console.WriteLine("Cleaning up old generated C# glue files...");
	        FileExporter.CleanOldExportedFiles();
	        
	        Console.WriteLine($"Export process completed successfully in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
	        stopwatch.Stop();

	        if (FileExporter.HasModifiedEngineGlue && BuildingEditor)
	        {
	            Console.WriteLine("Detected modified engine glue. Starting the build process...");
	            DotNetUtilities.BuildSolution(Path.Combine(ManagedPath, "UnrealSharp"));
	        }
	        
	        TryGenerateProject();
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

	static void TryGenerateProject()
	{
		string? projectName = Path.GetFileNameWithoutExtension(Factory.Session.ProjectFile);
		string projectPath = $"{Factory.Session.ProjectDirectory}/Script/Managed{projectName}.csproj";

		if (projectName == null || File.Exists(projectPath))
		{
			return;
		}
		
		string dotNetExe = DotNetUtilities.FindDotNetExecutable();

		string args = string.Empty;
		args += $"\"{PluginDirectory}/Binaries/Managed/UnrealSharpBuildTool.dll\"";
		args += " --Action GenerateProject";
		args += $" --EngineDirectory \"{Factory.Session.EngineDirectory}/\"";
		args += $" --ProjectDirectory \"{Factory.Session.ProjectDirectory}/\"";
		args += $" --ProjectName {projectName}";
		args += $" --PluginDirectory \"{PluginDirectory}\"";
		args += $" --DotNetPath \"{dotNetExe}\"";

		Process process = new Process();
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			WindowStyle = ProcessWindowStyle.Hidden,
			FileName = dotNetExe,
			Arguments = args
		};
		process.StartInfo = startInfo;
		process.Start();
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