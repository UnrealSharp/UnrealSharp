using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public static class GlueModuleFactory
{
	private static readonly Dictionary<string, List<string>> ModuleDependencies;

	static GlueModuleFactory()
	{
		ModuleDependencies = JsonUtilities.DeserializeObjectFromJson<Dictionary<string, List<string>>>(nameof(ModuleDependencies)) ?? new Dictionary<string, List<string>>();
	}

	public static void CreateGlueProjects()
	{
		bool anyProjectChanges = false;
		foreach (ModuleInfo moduleInfo in ModuleUtilities.PackageToModuleInfo.Values)
		{
			if (!moduleInfo.Module.ShouldExportPackage() || moduleInfo.IsPartOfEngine || moduleInfo.IsExtendingModule)
			{
				continue;
			}

			bool recentlyCreatedModule = false;
			if (!File.Exists(moduleInfo.CsProjPath))
			{
				CreateGlueModule(moduleInfo);
				recentlyCreatedModule = true;
			}

			List<string> pluginDependencies = moduleInfo.Dependencies.Select(m => m.CsProjPath).ToList();

			if (!recentlyCreatedModule && ModuleDependencies.TryGetValue(moduleInfo.ModuleName, out List<string>? existingDependencies))
			{
				if (existingDependencies.OrderBy(d => d).SequenceEqual(pluginDependencies.OrderBy(d => d)))
				{
					LoggerUtilities.LogUnrealSharpInfo(
						$"No changes in plugin dependencies for {moduleInfo.ModuleName}, skipping update.");
					continue;
				}
			}

			AddPluginDependencies(moduleInfo, pluginDependencies);
			ModuleDependencies[moduleInfo.ModuleName] = pluginDependencies;
			anyProjectChanges = true;
		}

		if (!anyProjectChanges)
		{
			return;
		}

		if (BuildGlueProjects())
		{
			JsonUtilities.SerializeObjectToJson(ModuleDependencies, nameof(ModuleDependencies));
		}
	}

	private static bool BuildGlueProjects()
	{
		if (GeneratorStatics.TargetType != TargetRules.TargetType.Editor)
		{
			return false;
		}

		List<KeyValuePair<string, string>> commandArgs = new List<KeyValuePair<string, string>>
		{
			new("TargetConfiguration", GeneratorStatics.TargetConfiguration.ToString()),
			new("TargetType", GeneratorStatics.TargetType.ToString()),
			new("OutputDirectory", PathUtilities.BuildOutputPath(GeneratorStatics.Factory.Session.ProjectDirectory!)),
		};

		UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("BuildUserGlue", commandArgs);
		return true;
	}

	private static void CreateGlueModule(ModuleInfo moduleInfo)
	{
		const string compileIncludeFolderVerb = "CompileIncludeFolder";

		List<KeyValuePair<string, string>> arguments = new List<KeyValuePair<string, string>>
		{
			new("ProjectName", moduleInfo.ModuleName),
			new("ProjectFolder", Path.GetDirectoryName(moduleInfo.CsProjPath)!),
			new("ProjectRoot", moduleInfo.ModuleRoot),
			new("SkipIncludeAnalyzers", "true"),
			new(compileIncludeFolderVerb, moduleInfo.ExtensionsDirectory),
		};

		foreach (ModuleInfo dependency in moduleInfo.Extensions)
		{
			arguments.Add(new KeyValuePair<string, string>(compileIncludeFolderVerb, dependency.GlueOutputDirectory));
			arguments.Add(new KeyValuePair<string, string>(compileIncludeFolderVerb, dependency.ExtensionsDirectory));

			List<string> extensionFolders = dependency.Module.GetAdditionalExtensionFolders();
			string scriptPath = dependency.ScriptPath;

			foreach (string folder in extensionFolders)
			{
				arguments.Add(new KeyValuePair<string, string>(compileIncludeFolderVerb,
					Path.Combine(scriptPath, folder)));
			}
		}

		if (moduleInfo.Module.IsEditorOnly())
		{
			arguments.Add(new KeyValuePair<string, string>("EditorOnly", "true"));
		}

		UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("GenerateProject", arguments);
	}

	private static void AddPluginDependencies(ModuleInfo moduleInfo, List<string>? pluginDependencies)
	{
		List<KeyValuePair<string, string>> arguments = new()
		{
			new KeyValuePair<string, string>("ProjectPath", moduleInfo.CsProjPath),
		};

		if (pluginDependencies != null)
		{
			foreach (string path in pluginDependencies)
			{
				arguments.Add(new KeyValuePair<string, string>("Dependencies", path));
			}
		}

		UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("UpdateProjectDependencies", arguments);
	}
}