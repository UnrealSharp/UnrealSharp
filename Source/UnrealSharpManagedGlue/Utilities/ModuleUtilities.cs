using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EpicGames.UHT.Types;

namespace UnrealSharpManagedGlue.Utilities;

public readonly struct ModuleInfo
{
	private readonly string _moduleName;
	private readonly string _moduleDirectory;
	public readonly HashSet<string>? Dependencies;
	public readonly UhtPackage Module;

	public ModuleInfo(string moduleName, string moduleDirectory, UhtPackage package, HashSet<string>? dependencies = null)
	{
		_moduleName = moduleName;
		_moduleDirectory = moduleDirectory;
		Dependencies = dependencies;
		Module = package;
	}

	public string ProjectName => $"{_moduleName}.Glue";
	public string ProjectFile => $"{ProjectName}.csproj";
	public string ScriptDirectory => Path.Combine(_moduleDirectory, "Script");
	public string ProjectDirectory => Path.Combine(ScriptDirectory, ProjectName);
	public string CsProjPath => Path.Combine(ProjectDirectory, ProjectFile);
    
	public bool ShouldFlatten => Module.ShouldFlattenGlue();
	public bool IsPartOfEngine => Module.IsPartOfEngine();
	public string ModuleRoot => _moduleDirectory;

	public string GlueBaseDirectory => Module.GetUhtBaseOutputDirectory();
	public string GlueModuleDirectory => Module.GetModuleUhtOutputDirectory();
    
	public bool IsUProject => _moduleDirectory.EndsWith(".uproject", StringComparison.OrdinalIgnoreCase);
}

public static class ModuleUtilities
{
	public static readonly Dictionary<UhtPackage, ModuleInfo> PackageToModuleInfo = new();
    
	private static readonly Dictionary<string, string> ExtractedEngineModules = new();
	private static readonly HashSet<UhtPackage> ProcessedPackages = new();

	static ModuleUtilities()
	{
		InitializeManifests();
		InitializeModules();
	}

	private static void InitializeManifests()
	{
		string? projectDirectory = GeneratorStatics.Factory.Session.ProjectDirectory;
		if (string.IsNullOrEmpty(projectDirectory))
		{
			return;
		}

		string pluginsDirectory = Path.Combine(projectDirectory, "Plugins");
		if (!Directory.Exists(pluginsDirectory))
		{
			return;
		}

		IEnumerable<string> manifestFiles = Directory.EnumerateFiles(pluginsDirectory, "*.ExtractedModules.json", SearchOption.AllDirectories);

		foreach (string manifestPath in manifestFiles)
		{
			try
			{
				string? configDir = Path.GetDirectoryName(manifestPath);
				string? pluginDir = Path.GetDirectoryName(configDir);

				if (string.IsNullOrEmpty(pluginDir))
				{
					continue;
				}

				using (FileStream stream = File.OpenRead(manifestPath))
				{
					List<string>? manifest = JsonSerializer.Deserialize<List<string>>(stream);
					if (manifest == null)
					{
						continue;
					}

					foreach (string moduleName in manifest)
					{
						ExtractedEngineModules[$"/Script/{moduleName}"] = pluginDir;
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to load manifest {manifestPath}: {e.Message}");
			}
		}
	}

	private static void InitializeModules()
	{
		foreach (UhtModule module in GeneratorStatics.Factory.Session.Modules)
		{
			foreach (UhtPackage package in module.Packages)
			{
				TryRegisterModule(package);
			}
		}
	}
    
	public static ModuleInfo GetModuleInfo(this UhtPackage package)
	{
		if (!PackageToModuleInfo.TryGetValue(package, out ModuleInfo moduleInfo))
		{
			throw new KeyNotFoundException($"ModuleInfo not found for package: {package.SourceName}");
		}
		
		return moduleInfo;
	}

	private static ModuleInfo? TryRegisterModule(UhtPackage targetPackage)
	{
		if (!targetPackage.ShouldExportPackage())
		{
			return null;
		}
		
		if (PackageToModuleInfo.TryGetValue(targetPackage, out ModuleInfo existingModule))
		{
			return existingModule;
		}

		string moduleName = targetPackage.GetModuleShortName();
		string modulePath;
		HashSet<string> dependencies = new HashSet<string>();

		if (targetPackage.IsPartOfEngine())
		{
			if (ExtractedEngineModules.TryGetValue(targetPackage.SourceName, out string? extractedModulePath))
			{
				DirectoryInfo pluginDir = new(extractedModulePath);
				moduleName = pluginDir.Name;
				modulePath = extractedModulePath;
			}
			else
			{
				modulePath = GeneratorStatics.BindingsProjectDirectory;
			}
		}
		else
		{
			modulePath = targetPackage.GetBaseDirectoryForPackage();
		}

		ModuleInfo moduleInfo = new ModuleInfo(moduleName, modulePath, targetPackage, dependencies);
		PackageToModuleInfo.Add(targetPackage, moduleInfo);
        
		if (ProcessedPackages.Add(targetPackage))
		{
			GatherDependencies(targetPackage, moduleInfo, dependencies);
		}
    
		return moduleInfo;
	}

	private static void GatherDependencies(UhtPackage sourcePackage, ModuleInfo info, HashSet<string> dependencies)
	{
		foreach (UhtType child in sourcePackage.Children)
		{
			ProcessTypeDependencies(child, sourcePackage, info, dependencies);
		}
	}

	private static void ProcessTypeDependencies(UhtType type, UhtPackage sourcePackage, ModuleInfo info, HashSet<string> dependencies)
	{
		if (type is UhtStruct uhtStruct)
		{
			if (uhtStruct.Super != null)
			{
				AddDependency(sourcePackage, uhtStruct.Super.Package, info, dependencies);
			}

			if (uhtStruct is UhtClass uhtClass)
			{
				foreach (UhtClass implementedInterface in uhtClass.GetInterfaces())
				{
					AddDependency(sourcePackage, implementedInterface.Package, info, dependencies);
				}
			}
		}
		else if (type is UhtProperty property)
		{
			foreach (UhtType referencedType in property.EnumerateReferencedTypes())
			{
				AddDependency(sourcePackage, referencedType.Package, info, dependencies);
			}
		}
        
		foreach (UhtType child in type.Children)
		{
			ProcessTypeDependencies(child, sourcePackage, info, dependencies);
		}
	}

	private static void AddDependency(UhtPackage sourcePackage, UhtPackage referencedPackage, ModuleInfo sourceModule, HashSet<string> uniqueDependencies)
	{
		if (referencedPackage == sourcePackage)
		{
			return;
		}
        
		if (referencedPackage.IsPartOfEngine() && !ExtractedEngineModules.ContainsKey(referencedPackage.SourceName))
		{
			return;
		}

		ModuleInfo? referencedModule = TryRegisterModule(referencedPackage);
		
		if (referencedModule == null)
		{
			return;
		}
		
		if (sourceModule.CsProjPath != referencedModule.Value.CsProjPath)
		{
			uniqueDependencies.Add(referencedModule.Value.CsProjPath);
		}
	}
}