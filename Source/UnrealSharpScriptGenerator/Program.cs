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
		Console.WriteLine("Starting export of C++ to C#...");
		Factory = factory;
		try
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			CSharpExporter exporter = new CSharpExporter();
			exporter.StartExport();
			stopwatch.Stop();
			Console.WriteLine($"Exported C++ to C# in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"An error occurred during the export: {ex.Message}");
			Console.WriteLine($"Stack Trace: {ex.StackTrace}");
		}
	}
}