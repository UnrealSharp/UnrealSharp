using System;
using System.IO;
using EpicGames.Core;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;
using UnrealBuildTool;

namespace UnrealSharpManagedGlue.Utilities;

public static class GeneratorStatics
{
	private static IUhtExportFactory? _factory;
	public static IUhtExportFactory Factory => _factory ?? throw new InvalidOperationException("GeneratorStatics not initialized");

	public static UHTManifest.Module PluginModule => Factory.PluginModule!;
	
	public static UhtPackage PluginPackage = null!;
	public static ModuleInfo PluginModuleInfo;

	public static string BindingsProjectDirectory { get; private set; } = "";
	public static string PluginsPath { get; private set; } = "";
	public static string ProjectName => Path.GetFileNameWithoutExtension(Factory.Session.ProjectFile!);
	
	public static UhtClass BlueprintFunctionLibrary { get; private set; } = null!;

	public static string PluginDirectory { get; private set; } = "";
	public static string EngineDirectory => Factory.Session.EngineDirectory!;
	
	public static string ManagedBinariesPath { get; private set; } = "";
	public static string ManagedPath { get; private set; } = "";
	public static string ScriptFolder { get; private set; } = "";
	
	public static TargetType BuildTarget { get; private set; }
	
	public static void Initialize(IUhtExportFactory factory)
	{
		_factory = factory;
		
		PluginDirectory = ScriptGeneratorUtilities.TryGetPluginStringDefine("PLUGIN_PATH");
		BindingsProjectDirectory = ScriptGeneratorUtilities.TryGetPluginStringDefine("GENERATED_GLUE_PATH");
		BuildTarget = (TargetType) ScriptGeneratorUtilities.TryGetPluginIntDefine("BUILD_TARGET");
		
		ScriptFolder = Path.Combine(Factory.Session.ProjectDirectory!, "Script");
		PluginsPath = Path.Combine(Factory.Session.ProjectDirectory!, "Plugins");
		ManagedBinariesPath = Path.Combine(PluginDirectory, "Binaries", "Managed");
		ManagedPath = Path.Combine(PluginDirectory, "Managed");
		
		BlueprintFunctionLibrary = (Factory.Session.FindType(null, UhtFindOptions.SourceName | UhtFindOptions.Class, "UBlueprintFunctionLibrary") as UhtClass)!;
		
		ModuleInfo moduleInfo = ModuleUtilities.GetModuleInfo($"/Script/{factory.PluginModule!.Name}");
		PluginPackage = moduleInfo.Module;
		PluginModuleInfo = moduleInfo;
	}
}