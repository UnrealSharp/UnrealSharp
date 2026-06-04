using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using EpicGames.UHT.Types;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharpManagedGlue.Utilities;

public sealed class ModuleInfo
{
	public UhtPackage Module { get; }

	public string ModuleName { get; }
	public string ModuleRoot { get; }
	public string ScriptPath { get; }

	public bool IsExtendingModule { get; }
	public bool IsEngineModule { get; }

	public IReadOnlyList<ModuleInfo> Extensions { get; private set; } = [];
	public bool HasExtensions => Extensions.Count > 0;
	
	public bool EmitsToProjectDirectory { get; private set; }
	
	public bool IsPartOfEngine => IsEngineModule && !EmitsToProjectDirectory;

	public string GlueOutputDirectory { get; private set; } = string.Empty;
	public string CsProjPath { get; private set; } = string.Empty;
	public string ExtensionsDirectory => Path.Combine(ScriptPath, "Extensions");

	internal readonly HashSet<ModuleInfo> DirectDependencies = new();
	internal readonly HashSet<ModuleInfo> DependencyModules = new();

	public IReadOnlyCollection<ModuleInfo> Dependencies => DependencyModules;

	internal ModuleInfo(string moduleName, string moduleRoot, UhtPackage package)
	{
		Module = package;
		ModuleName = moduleName;
		ModuleRoot = moduleRoot;
		IsExtendingModule = package.GetPackageExtensions() != string.Empty;
		IsEngineModule = package.IsPartOfEngine();
		ScriptPath = Path.Combine(moduleRoot, CommonUnrealSharpSettings.ScriptDirectoryName, moduleName);
	}
	
	internal void ResolveExtensions(IReadOnlyList<ModuleInfo>? extensions)
	{
		Extensions = extensions ?? [];
		EmitsToProjectDirectory = IsEngineModule && HasExtensions;
	}
	
	internal bool MarkEmitsToProjectDirectory()
	{
		if (EmitsToProjectDirectory)
		{
			return false;
		}

		EmitsToProjectDirectory = true;
		return true;
	}
	
	internal void ResolveOutputPaths()
	{
		string root = EmitsToProjectDirectory ? GeneratorStatics.Factory.Session.ProjectDirectory! : ModuleRoot;

		GlueOutputDirectory = PathUtilities.GetUhtGeneratedModuleOutputPath(root, GeneratorStatics.TargetType, ModuleName);
		CsProjPath = Path.Combine(GlueOutputDirectory, $"{ModuleName}.csproj");
	}

	public override string ToString() => ModuleName;
}

public static class ModuleUtilities
{
	public static readonly Dictionary<UhtPackage, ModuleInfo> PackageToModuleInfo = new();
	private static readonly Dictionary<string, ModuleInfo> BySourceName = new(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, List<ModuleInfo>> ModuleExtensions = new();

	static ModuleUtilities()
	{
		Build();
	}

	private static void Build()
	{
		Dictionary<UhtPackage, List<UhtPackage>> referencedPackages = DiscoverModules();
		
		foreach (ModuleInfo module in PackageToModuleInfo.Values)
		{
			ModuleExtensions.TryGetValue(module.ModuleName, out List<ModuleInfo>? extensions);
			module.ResolveExtensions(extensions);
		}
		
		foreach ((UhtPackage package, List<UhtPackage> references) in referencedPackages)
		{
			ModuleInfo sourceModule = PackageToModuleInfo[package];

			foreach (UhtPackage referenced in references)
			{
				AddDirectDependency(package, referenced, sourceModule);
			}
		}
		
		foreach (ModuleInfo module in PackageToModuleInfo.Values)
		{
			FlattenDependencies(module);
		}
		
		PropagateProjectRedirection();
		
		foreach (ModuleInfo module in PackageToModuleInfo.Values)
		{
			module.DependencyModules.RemoveWhere(static dependency => dependency.IsPartOfEngine);
			module.ResolveOutputPaths();
		}
	}

	public static ModuleInfo GetModuleInfo(this UhtPackage package)
	{
		if (PackageToModuleInfo.TryGetValue(package, out ModuleInfo? moduleInfo))
		{
			return moduleInfo;
		}

		throw new KeyNotFoundException($"No ModuleInfo registered for package '{package.SourceName}'.");
	}

	public static ModuleInfo GetModuleInfo(string packageName)
	{
		if (TryGetModuleInfo(packageName, out ModuleInfo? moduleInfo))
		{
			return moduleInfo;
		}

		throw new KeyNotFoundException($"No ModuleInfo registered for package name '{packageName}'.");
	}

	public static bool TryGetModuleInfo(string packageName, [NotNullWhen(true)] out ModuleInfo? moduleInfo)
	{
		string sourceName = packageName.StartsWith("/Script/", StringComparison.OrdinalIgnoreCase) ? packageName : $"/Script/{packageName}";
		return BySourceName.TryGetValue(sourceName, out moduleInfo);
	}

	private static Dictionary<UhtPackage, List<UhtPackage>> DiscoverModules()
	{
		Dictionary<UhtPackage, List<UhtPackage>> referencedPackages = new();
		Queue<UhtPackage> pending = new();
		HashSet<UhtPackage> seen = new();

		foreach (UhtModule module in GeneratorStatics.Factory.Session.Modules)
		{
			foreach (UhtPackage package in module.Packages)
			{
				pending.Enqueue(package);
			}
		}

		while (pending.Count > 0)
		{
			UhtPackage package = pending.Dequeue();

			if (!seen.Add(package))
			{
				continue;
			}

			if (TryRegisterModule(package) == null)
			{
				continue;
			}

			List<UhtPackage> references = CollectReferencedPackages(package);
			referencedPackages[package] = references;

			foreach (UhtPackage reference in references)
			{
				if (!seen.Contains(reference))
				{
					pending.Enqueue(reference);
				}
			}
		}

		return referencedPackages;
	}

	private static ModuleInfo? TryRegisterModule(UhtPackage package)
	{
		if (PackageToModuleInfo.TryGetValue(package, out ModuleInfo? existing))
		{
			return existing;
		}

		if (!package.ShouldExportPackage())
		{
			return null;
		}

		string moduleName = package.GetModuleShortName();
		string moduleRoot = package.IsPartOfEngine() ? GeneratorStatics.PluginDirectory : package.GetBaseDirectoryForPackage();

		ModuleInfo moduleInfo = new(moduleName, moduleRoot, package);

		PackageToModuleInfo.Add(package, moduleInfo);
		BySourceName[package.SourceName] = moduleInfo;
		RegisterExtensionTargets(moduleInfo, package);

		return moduleInfo;
	}

	private static void RegisterExtensionTargets(ModuleInfo moduleInfo, UhtPackage package)
	{
		string extendedModuleName = package.GetPackageExtensions();
		if (string.IsNullOrEmpty(extendedModuleName))
		{
			return;
		}
		
		if (!ModuleExtensions.TryGetValue(extendedModuleName, out List<ModuleInfo>? extenders))
		{
			extenders = new List<ModuleInfo>();
			ModuleExtensions[extendedModuleName] = extenders;
		}

		extenders.Add(moduleInfo);
	}

	private static List<UhtPackage> CollectReferencedPackages(UhtPackage package)
	{
		HashSet<UhtPackage> references = new();

		foreach (UhtType child in package.Children)
		{
			CollectTypeReferences(child, references);
		}

		return references.ToList();
	}

	private static void CollectTypeReferences(UhtType type, HashSet<UhtPackage> references)
	{
		if (type is UhtStruct uhtStruct)
		{
			if (uhtStruct.Super != null)
			{
				references.Add(uhtStruct.Super.Package);
			}

			if (uhtStruct is UhtClass uhtClass)
			{
				foreach (UhtClass implementedInterface in uhtClass.GetInterfaces())
				{
					references.Add(implementedInterface.Package);
				}
			}
		}
		else if (type is UhtProperty property)
		{
			foreach (UhtType referencedType in property.EnumerateReferencedTypes())
			{
				references.Add(referencedType.Package);
			}
		}

		foreach (UhtType child in type.Children)
		{
			CollectTypeReferences(child, references);
		}
	}

	private static void AddDirectDependency(UhtPackage sourcePackage, UhtPackage referencedPackage, ModuleInfo sourceModule)
	{
		if (referencedPackage == sourcePackage)
		{
			return;
		}

		if (!PackageToModuleInfo.TryGetValue(referencedPackage, out ModuleInfo? referencedModule))
		{
			return;
		}

		if (referencedModule == sourceModule)
		{
			return;
		}

		sourceModule.DirectDependencies.Add(referencedModule);
	}

	private static void FlattenDependencies(ModuleInfo module)
	{
		HashSet<ModuleInfo> visited = new();

		foreach (ModuleInfo dependency in module.DirectDependencies)
		{
			CollectFlattenedDependency(dependency, module.DependencyModules, visited);
		}
	}

	private static void CollectFlattenedDependency(ModuleInfo dependency, HashSet<ModuleInfo> result, HashSet<ModuleInfo> visited)
	{
		if (!dependency.IsExtendingModule)
		{
			result.Add(dependency);
			return;
		}

		if (!visited.Add(dependency))
		{
			return;
		}

		foreach (ModuleInfo transitive in dependency.DirectDependencies)
		{
			CollectFlattenedDependency(transitive, result, visited);
		}
	}
	
	private static void PropagateProjectRedirection()
	{
		Dictionary<ModuleInfo, List<ModuleInfo>> dependents = new();
		Queue<ModuleInfo> queue = new();

		foreach (ModuleInfo module in PackageToModuleInfo.Values)
		{
			foreach (ModuleInfo dependency in module.DependencyModules)
			{
				if (!dependents.TryGetValue(dependency, out List<ModuleInfo>? referencingModules))
				{
					referencingModules = new List<ModuleInfo>();
					dependents[dependency] = referencingModules;
				}

				referencingModules.Add(module);
			}
			
			if (module.EmitsToProjectDirectory)
			{
				queue.Enqueue(module);
			}
		}

		while (queue.Count > 0)
		{
			ModuleInfo redirected = queue.Dequeue();

			if (!dependents.TryGetValue(redirected, out List<ModuleInfo>? referencingModules))
			{
				continue;
			}

			foreach (ModuleInfo referencing in referencingModules)
			{
				if (referencing.IsEngineModule && referencing.MarkEmitsToProjectDirectory())
				{
					queue.Enqueue(referencing);
				}
			}
		}
	}
}