using System;
using System.Diagnostics;
using EpicGames.Core;
using EpicGames.UHT.Tables;
using EpicGames.UHT.Utils;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator;

[UnrealHeaderTool]
public static class Program
{
	public static IUhtExportFactory Factory { get; private set; } = null!;
	public static UHTManifest.Module PluginModule => Factory.PluginModule!;
	public static string GeneratedGluePath => ScriptGeneratorUtilities.TryGetPluginDefine("GENERATED_GLUE_PATH");

	[UhtExporter(Name = "CSharpForUE", Description = "Exports C++ to C# code", Options = UhtExporterOptions.Default, ModuleName = "CSharpForUE")]
	private static void Main(IUhtExportFactory factory)
	{
		Console.WriteLine("Initializing C# exporter...");
		Factory = factory;
		
		try
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			
			CSharpExporter.StartExport();
        
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
}