using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public static class ModuleFactory
{
	const string CompileIncludeFolderVerb = "CompileIncludeFolder";
	
	private static readonly Dictionary<string, List<string>> ModuleDependencies;

	static ModuleFactory()
	{
		ModuleDependencies = JsonUtilities.DeserializeObjectFromJson<Dictionary<string, List<string>>>(nameof(ModuleDependencies)) ?? new Dictionary<string, List<string>>();
	}

	public static void SyncModuleProjects()
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
				GenerateModuleProject(moduleInfo);
				recentlyCreatedModule = true;
			}

			List<string> pluginDependencies = moduleInfo.Dependencies.Select(m => m.CsProjPath).ToList();

			if (!recentlyCreatedModule && ModuleDependencies.TryGetValue(moduleInfo.ModuleName, out List<string>? existingDependencies))
			{
				if (existingDependencies.OrderBy(d => d).SequenceEqual(pluginDependencies.OrderBy(d => d)))
				{
					LoggerUtilities.LogUnrealSharpInfo($"No changes in plugin dependencies for {moduleInfo.ModuleName}, skipping update.");
					continue;
				}
			}

			UpdateModuleDependencies(moduleInfo, pluginDependencies);
			ModuleDependencies[moduleInfo.ModuleName] = pluginDependencies;
			anyProjectChanges = true;
		}

		if (!anyProjectChanges)
		{
			return;
		}

		if (CompileGlueProjects())
		{
			JsonUtilities.SerializeObjectToJson(ModuleDependencies, nameof(ModuleDependencies));
		}
	}

	private static bool CompileGlueProjects()
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

	private static void GenerateModuleProject(ModuleInfo moduleInfo)
	{
		List<KeyValuePair<string, string>> arguments = new List<KeyValuePair<string, string>>
		{
			new("ProjectName", moduleInfo.ModuleName),
			new("ProjectFolder", Path.GetDirectoryName(moduleInfo.CsProjPath)!),
			new("ProjectRoot", moduleInfo.ModuleRoot),
			new("SkipIncludeAnalyzers", "true"),
			new(CompileIncludeFolderVerb, moduleInfo.ExtensionsDirectory),
		};
		
		AppendExtensionIncludeFolders(moduleInfo, arguments);

		if (moduleInfo.Module.IsEditorOnly())
		{
			arguments.Add(new KeyValuePair<string, string>("EditorOnly", "true"));
		}

		UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("GenerateProject", arguments);
	}

	private static void AppendExtensionIncludeFolders(ModuleInfo moduleInfo, List<KeyValuePair<string, string>> arguments)
	{
		foreach (ModuleInfo extensionModule in moduleInfo.Extensions)
		{
			arguments.Add(new KeyValuePair<string, string>(CompileIncludeFolderVerb, extensionModule.GlueOutputDirectory));
			arguments.Add(new KeyValuePair<string, string>(CompileIncludeFolderVerb, extensionModule.ExtensionsDirectory));

			List<string> extensionFolders = extensionModule.Module.GetAdditionalExtensionFolders();
			string scriptPath = extensionModule.ScriptPath;

			foreach (string folder in extensionFolders)
			{
				arguments.Add(new KeyValuePair<string, string>(CompileIncludeFolderVerb, Path.Combine(scriptPath, folder)));
			}
		}
	}

	private static void UpdateModuleDependencies(ModuleInfo moduleInfo, List<string>? pluginDependencies)
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