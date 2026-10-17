using System;
using System.IO;
using EpicGames.Core;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharpManagedGlue.Utilities;

public class GeneratorStatics
{
	private IUhtExportFactory? _factory;
	public IUhtExportFactory Factory => _factory ?? throw new InvalidOperationException("GeneratorStatics not initialized");

	public UHTManifest.Module PluginModule => Factory.PluginModule!;
	
	public ModuleInfo PluginModuleInfo { get; private set; } = null!;
	
	public UhtClass BlueprintFunctionLibrary { get; private set; } = null!;

	public string PluginDirectory { get; private set; } = "";
	public string EngineDirectory => Factory.Session.EngineDirectory!;
	
	public string ManagedPath { get; private set; } = "";
	
	public TargetType TargetType { get; private set; }
	public UnrealTargetConfiguration TargetConfiguration;
	
	public void Initialize(IUhtExportFactory factory)
	{
		if (_factory != null)
		{
			factory.Session.LogError("UnrealSharp generator data was initialized more than once for this UHT session.");
			throw new InvalidOperationException("GeneratorStatics already initialized");
		}

		_factory = factory;
		
		PluginDirectory = ScriptGeneratorUtilities.TryGetPluginStringDefine("PLUGIN_PATH");
		SessionStatic.Settings.InitializeConfigFile(Factory.Session.ProjectDirectory!, PluginDirectory);
		
		TargetType = (TargetType) ScriptGeneratorUtilities.TryGetPluginIntDefine("TARGET_TYPE");
		TargetConfiguration = (UnrealTargetConfiguration) ScriptGeneratorUtilities.TryGetPluginIntDefine("TARGET_CONFIGURATION");
		
		ManagedPath = Path.Combine(PluginDirectory, "Managed");
		
		BlueprintFunctionLibrary = (Factory.Session.FindType(null, UhtFindOptions.SourceName | UhtFindOptions.Class, "UBlueprintFunctionLibrary") as UhtClass)!;
		
		SessionStatic.Modules.Initialize();
		PluginModuleInfo = SessionStatic.Modules.GetModuleInfo(factory.PluginModule!.Name);

		PathUtilities.InitPaths(factory.Session.EngineDirectory!);
	}
}
