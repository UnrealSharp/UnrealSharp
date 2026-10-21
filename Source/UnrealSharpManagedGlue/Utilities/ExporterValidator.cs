using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace UnrealSharpManagedGlue.Utilities;

public static class ExporterValidator
{
	private static string AssemblyModificationTimeFile => Path.Combine(GeneratorStatics.PluginModule.OutputDirectory, "GlueGeneratorTimestamp");

	static bool HasAssemblyRecentlyChanged()
	{
		string executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
		DateTime executingAssemblyLastWriteTime = File.GetLastWriteTimeUtc(executingAssemblyPath);
		
		if (!File.Exists(AssemblyModificationTimeFile))
		{
			return true;
		}

		string savedTimestamp = File.ReadAllText(AssemblyModificationTimeFile);
		if (!DateTime.TryParse(savedTimestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime savedTimestampUtc))
		{
			return true;
		}

		return executingAssemblyLastWriteTime > savedTimestampUtc;
	}

	public static void ValidateExporter()
	{
		ConsoleUtilities.Log("Validating exporter...");

		if (!HasAssemblyRecentlyChanged())
		{
			PackageHeadersTracker.DeserializeModuleHeaders();
			return;
		}

		ConsoleUtilities.Log("Exporter is stale. Re-exporting all bindings...");
		CleanModules();
		CreateTimestamp();
	}

	private static void CreateTimestamp()
	{
		string executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
		DateTime executingAssemblyLastWriteTime = File.GetLastWriteTimeUtc(executingAssemblyPath);

		string? directory = Path.GetDirectoryName(AssemblyModificationTimeFile);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		File.WriteAllText(AssemblyModificationTimeFile, executingAssemblyLastWriteTime.ToString("o"));
	}

	private static void CleanModules()
	{
		IEnumerable<ModuleInfo> modules = ModuleUtilities.PackageToModuleInfo.Values;

		foreach (ModuleInfo module in modules)
		{
			FileExporter.CleanGeneratedFolder(module.Module.GetPackageOutputDirectory());

			if (module.IsPartOfEngine || !File.Exists(module.CsProjPath))
			{
				continue;
			}

			File.Delete(module.CsProjPath);
		}
	}
}