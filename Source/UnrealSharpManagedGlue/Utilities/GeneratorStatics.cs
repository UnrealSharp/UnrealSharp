using System;
using System.IO;
using EpicGames.Core;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;

namespace UnrealSharpManagedGlue.Utilities;

public static class GeneratorStatics
{
	private static IUhtExportFactory? _factory;
	public static IUhtExportFactory Factory => _factory ?? throw new InvalidOperationException("GeneratorStatics not initialized");

	public static UHTManifest.Module PluginModule => Factory.PluginModule!;

	public static string EngineGluePath { get; private set; } = "";
	public static string PluginsPath { get; private set; } = "";
	public static string ProjectName => Path.GetFileNameWithoutExtension(Factory.Session.ProjectFile!);
	
	public static UhtClass BlueprintFunctionLibrary { get; private set; } = null!;

	public static string PluginDirectory { get; private set; } = "";
	public static string EngineDirectory => Factory.Session.EngineDirectory!;
	
	public static string ManagedBinariesPath { get; private set; } = "";
	public static string ManagedPath { get; private set; } = "";
	public static string ScriptFolder { get; private set; } = "";
	
	public static bool IsBuildingEditor { get; private set; } = false;
	
	public static void Initialize(IUhtExportFactory factory)
	{
		if (_factory != null)
		{
			throw new InvalidOperationException("GeneratorStatics already initialized");
		}
		
		_factory = factory;
		
		PluginDirectory = ScriptGeneratorUtilities.TryGetPluginDefine("PLUGIN_PATH");
		
		EngineGluePath = ScriptGeneratorUtilities.TryGetPluginDefine("GENERATED_GLUE_PATH");
		
		IsBuildingEditor = ScriptGeneratorUtilities.TryGetPluginDefine("BUILDING_EDITOR") == "1";
		
		ScriptFolder = Path.Combine(Factory.Session.ProjectDirectory!, "Script");
		PluginsPath = Path.Combine(Factory.Session.ProjectDirectory!, "Plugins");
		ManagedBinariesPath = Path.Combine(PluginDirectory, "Binaries", "Managed");
		ManagedPath = Path.Combine(PluginDirectory, "Managed");
		
		BlueprintFunctionLibrary = (Factory.Session.FindType(null, UhtFindOptions.SourceName | UhtFindOptions.Class, "UBlueprintFunctionLibrary") as UhtClass)!;
	}
}