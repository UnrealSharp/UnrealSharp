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
	public static string EngineGluePath => ScriptGeneratorUtilities.TryGetPluginDefine("GENERATED_GLUE_PATH");
	public static string PluginDirectory => ScriptGeneratorUtilities.TryGetPluginDefine("PLUGIN_PATH");
	public static string ManagedBinariesPath => Path.Combine(PluginDirectory, "Binaries", "Managed");
	public static string ManagedPath => Path.Combine(PluginDirectory, "Managed");
	public static string ScriptFolder
	{
		get
		{
			DirectoryInfo unrealSharpDirectory = Directory.GetParent(PluginDirectory).Parent;
			string scriptFolderPath = Path.Combine(unrealSharpDirectory.FullName, "Script");
			return scriptFolderPath;
		}
	} 
	public static string ProjectGluePath => Path.Combine(ScriptFolder, "obj", "generated");
	
	public static UhtClass BlueprintFunctionLibrary = null!;

	[UhtExporter(Name = "CSharpForUE", Description = "Exports C++ to C# code", Options = UhtExporterOptions.Default,
		ModuleName = "CSharpForUE",
		CppFilters = new[] { "*.generated.cs" }, HeaderFilters = new[] { "*.generated.cs" },
		OtherFilters = new[] { "*.generated.cs" })]
	private static void Main(IUhtExportFactory factory)
	{
		Console.WriteLine("Initializing C# exporter...");
		Factory = factory;
		
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
			
			if (FileExporter.HasModifiedEngineGlue)
			{
				Console.WriteLine("Compiling C# bindings...");
				string bindingsPath = Path.Combine(ManagedPath, "UnrealSharp");
				PublishSolution(bindingsPath);
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
	static string FindDotNetExecutable()
	{
		var pathVariable = Environment.GetEnvironmentVariable("PATH");
    
		if (pathVariable == null)
		{
			return null;
		}
    
		var paths = pathVariable.Split(Path.PathSeparator);
    
		foreach (var path in paths)
		{
			// This is a hack to avoid using the dotnet.exe from the Unreal Engine installation directory.
			// Can't use the dotnet.exe from the Unreal Engine installation directory because it's .NET 6.0
			if (!path.Contains(@"\dotnet\"))
			{
				continue;
			}
			
			var dotnetExePath = Path.Combine(path, "dotnet.exe");
			
			if (File.Exists(dotnetExePath))
			{
				return dotnetExePath;
			}
		}

		throw new Exception("Couldn't find dotnet.exe!");
	}
	static void PublishSolution(string projectRootDirectory)
	{
		if (!Directory.Exists(projectRootDirectory))
		{
			throw new Exception($"Couldn't find project root directory: {projectRootDirectory}");
		}
		
		string dotnetPath = FindDotNetExecutable();
		
		Process process = new Process();
		process.StartInfo.FileName = dotnetPath;
		
		process.StartInfo.ArgumentList.Add("publish");
		process.StartInfo.ArgumentList.Add($"\"{projectRootDirectory}\"");
		
		process.StartInfo.ArgumentList.Add($"-p:PublishDir=\"{ManagedBinariesPath}\"");
		
		process.Start();
		process.WaitForExit();
		
		if (process.ExitCode != 0)
		{
			throw new Exception($"Failed to publish solution: {projectRootDirectory}");
		}
	}
}