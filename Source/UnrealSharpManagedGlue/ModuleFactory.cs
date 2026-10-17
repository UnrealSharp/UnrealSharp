using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public class ModuleFactory
{
	const string CompileIncludeFolderVerb = "CompileIncludeFolder";
	
	private Dictionary<string, List<string>> ModuleDependencies = new();

	public void Initialize()
	{
		ModuleDependencies = JsonUtilities.DeserializeObjectFromJson<Dictionary<string, List<string>>>(nameof(ModuleDependencies)) ?? new Dictionary<string, List<string>>();
	}

	public void SyncModuleProjects()
	{
		bool anyProjectChanges = false;
		bool anyGlueDirty = false;

		foreach (ModuleInfo moduleInfo in SessionStatic.Modules.PackageToModuleInfo.Values)
		{
			if (!moduleInfo.Module.ShouldExportPackage() || moduleInfo.IsPartOfEngine || moduleInfo.IsExtendingModule)
			{
				continue;
			}

			if (moduleInfo.IsDirty)
			{
				anyGlueDirty = true;
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
					continue;
				}
			}

			UpdateModuleDependencies(moduleInfo, pluginDependencies);
			ModuleDependencies[moduleInfo.ModuleName] = pluginDependencies;
			anyProjectChanges = true;
		}
		
		if (!anyProjectChanges && !anyGlueDirty)
		{
			return;
		}

		if (CompileGlueProjects())
		{
			JsonUtilities.SerializeObjectToJson(ModuleDependencies, nameof(ModuleDependencies));
		}
	}

	private bool CompileGlueProjects()
	{
		if (SessionStatic.Generator.TargetType != TargetRules.TargetType.Editor)
		{
			return false;
		}

		List<KeyValuePair<string, string>> commandArgs = new List<KeyValuePair<string, string>>
		{
			new("TargetConfiguration", SessionStatic.Generator.TargetConfiguration.ToString()),
			new("TargetType", SessionStatic.Generator.TargetType.ToString()),
			new("OutputDirectory", PathUtilities.BuildOutputPath(SessionStatic.Generator.Factory.Session.ProjectDirectory!)),
			new("AddReferences", "true")
		};

		UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("BuildUserGlue", commandArgs);
		return true;
	}

	private void GenerateModuleProject(ModuleInfo moduleInfo)
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

	private void AppendExtensionIncludeFolders(ModuleInfo moduleInfo, List<KeyValuePair<string, string>> arguments)
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

	private void UpdateModuleDependencies(ModuleInfo moduleInfo, List<string>? pluginDependencies)
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
