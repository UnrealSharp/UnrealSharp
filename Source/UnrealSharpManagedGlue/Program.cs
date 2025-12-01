using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Tables;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;
using UnrealSharp.Shared;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator;

[UnrealHeaderTool]
public static class Program
{
    public static IUhtExportFactory Factory { get; private set; } = null!;
    public static UHTManifest.Module PluginModule => Factory.PluginModule!;

    public static string EngineGluePath { get; private set; } = "";
    public static string PluginsPath { get; private set; } = "";
    public static string ProjectName => Path.GetFileNameWithoutExtension(Factory.Session.ProjectFile!);

    public static bool BuildingEditor { get; private set; }
    public static UhtClass BlueprintFunctionLibrary { get; private set; } = null!;

    public static string PluginDirectory { get; private set; } = "";
    public static string ManagedBinariesPath { get; private set; } = "";
    public static string ManagedPath { get; private set; } = "";
    public static string ScriptFolder { get; private set; } = "";
    public static ImmutableArray<ProjectDirInfo> PluginDirs { get; private set; }

    [UhtExporter(Name = "UnrealSharpCore", Description = "Exports C++ to C# code", Options = UhtExporterOptions.Default,
        ModuleName = "UnrealSharpCore")]
    private static void Main(IUhtExportFactory factory)
    {
        Console.WriteLine("Initializing C# exporter...");
        
        Factory = factory;
        
        InitializeStatics();
        CacheBlueprintFunctionLibrary();
        
        USharpBuildToolUtilities.CompileUSharpBuildTool();

        try
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            CSharpExporter.StartExport();
            stopwatch.Stop();
            
            Console.WriteLine($"Export process completed successfully in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");

            if (CSharpExporter.HasModifiedEngineGlue && BuildingEditor)
            {
                Console.WriteLine("Detected modifications to Engine glue, rebuilding UnrealSharp solution...");
                DotNetUtilities.BuildSolution(Path.Combine(ManagedPath, "UnrealSharp"));
            }
            
            USharpBuildToolUtilities.CreateGlueProjects();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("An error occurred during the export process:");
            Console.WriteLine($"Error Message: {ex.Message}");
            Console.WriteLine("Stack Trace:");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
        }
    }

    private static void InitializeStatics()
    {
        PluginDirectory = ScriptGeneratorUtilities.TryGetPluginDefine("PLUGIN_PATH");

        string projectDirectory = Factory.Session.ProjectDirectory!;
		ScriptFolder = Path.Combine(projectDirectory, "Script");
        PluginsPath = Path.Combine(projectDirectory, "Plugins");

        EngineGluePath = ScriptGeneratorUtilities.TryGetPluginDefine("GENERATED_GLUE_PATH");

        ManagedBinariesPath = Path.Combine(PluginDirectory, "Binaries", "Managed");

        ManagedPath = Path.Combine(PluginDirectory, "Managed");

        BuildingEditor = ScriptGeneratorUtilities.TryGetPluginDefine("BUILDING_EDITOR") == "1";

        DirectoryInfo pluginsDir = new DirectoryInfo(PluginsPath);

        PluginDirs = pluginsDir.GetFiles("*.uplugin", SearchOption.AllDirectories)
                .Where(x => x.Directory!.GetDirectories("Source").Length != 0)
                .Select(x => new ProjectDirInfo(Path.GetFileNameWithoutExtension(x.Name), x.DirectoryName!))
                .Where(x => x.GlueProjectName != "UnrealSharp")
                .ToImmutableArray();
    }
    
    private static void CacheBlueprintFunctionLibrary()
    {
        UhtType? foundType = Factory.Session.FindType(null, UhtFindOptions.SourceName | UhtFindOptions.Class, "UBlueprintFunctionLibrary");
        if (foundType is not UhtClass blueprintFunctionLibrary)
        {
            throw new InvalidOperationException("Failed to find UBlueprintFunctionLibrary class.");
        }

        BlueprintFunctionLibrary = blueprintFunctionLibrary;
    }
}
