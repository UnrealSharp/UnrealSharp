using System;
using System.IO;
using EpicGames.Core;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharpManagedGlue.Utilities;

public static class GeneratorStatics
{
	private static IUhtExportFactory? _factory;
	public static IUhtExportFactory Factory => _factory ?? throw new InvalidOperationException("GeneratorStatics not initialized");

	public static UHTManifest.Module PluginModule => Factory.PluginModule!;
	
	public static ModuleInfo PluginModuleInfo { get; private set; } = null!;
	
	public static UhtClass BlueprintFunctionLibrary { get; private set; } = null!;

	public static string PluginDirectory { get; private set; } = "";
	public static string EngineDirectory => Factory.Session.EngineDirectory!;
	
	public static string ManagedPath { get; private set; } = "";
	
	public static TargetType TargetType { get; private set; }
	public static UnrealTargetConfiguration TargetConfiguration;
	
	public static void Initialize(IUhtExportFactory factory)
	{
		_factory = factory;
		
		PluginDirectory = ScriptGeneratorUtilities.TryGetPluginStringDefine("PLUGIN_PATH");
		UnrealSharpSettingsUtilities.InitializeConfigFile(Factory.Session.ProjectDirectory!, PluginDirectory);
		
		TargetType = (TargetType) ScriptGeneratorUtilities.TryGetPluginIntDefine("TARGET_TYPE");
		TargetConfiguration = (UnrealTargetConfiguration) ScriptGeneratorUtilities.TryGetPluginIntDefine("TARGET_CONFIGURATION");
		
		ManagedPath = Path.Combine(PluginDirectory, "Managed");
		
		BlueprintFunctionLibrary = (Factory.Session.FindType(null, UhtFindOptions.SourceName | UhtFindOptions.Class, "UBlueprintFunctionLibrary") as UhtClass)!;
		
		PluginModuleInfo = ModuleUtilities.GetModuleInfo(factory.PluginModule!.Name);

		PathUtilities.InitPaths(factory.Session.EngineDirectory!);
	}
}