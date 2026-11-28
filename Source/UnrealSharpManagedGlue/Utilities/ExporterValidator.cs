using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace UnrealSharpManagedGlue.Utilities;

public static class ExporterValidator
{
	private static string AssemblyModificationTimeFile => Path.Combine(GeneratorStatics.PluginModule.OutputDirectory, "GlueGeneratorTimestamp");
	private static string HeaderExclusionsFile => Path.Combine(GeneratorStatics.PluginModule.OutputDirectory, "GlueGeneratorHeaderExclusions.json");

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

		string exclusions = JsonSerializer.Serialize(UnrealSharpSettingsUtilities.ExcludedHeaderPaths);
		bool exclusionsChanged = !File.Exists(HeaderExclusionsFile) || File.ReadAllText(HeaderExclusionsFile) != exclusions;
		if (!HasAssemblyRecentlyChanged() && !exclusionsChanged)
		{
			PackageHeadersTracker.DeserializeModuleHeaders();
			return;
		}

		ConsoleUtilities.Log("Exporter is stale. Re-exporting all bindings...");
		CleanModules();
		CreateTimestamp();
		// Changing an exclusion must also regenerate headers that were previously skipped.
		File.WriteAllText(HeaderExclusionsFile, exclusions);
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
