using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EpicGames.UHT.Tables;
using EpicGames.UHT.Utils;
using UnrealSharp.Shared;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

[UnrealHeaderTool]
public static class Program
{
    public static ImmutableArray<ProjectDirInfo> PluginDirs { get; private set; }

    [UhtExporter(Name = "UnrealSharpCore", Description = "Exports C++ to C# code", Options = UhtExporterOptions.Default, ModuleName = "UnrealSharpCore")]
    private static void Main(IUhtExportFactory factory)
    {
        Console.WriteLine("Initializing C# exporter...");
        GeneratorStatics.Initialize(factory);
        
        DirectoryInfo pluginsDir = new DirectoryInfo(GeneratorStatics.PluginsPath);

        PluginDirs = pluginsDir.GetFiles("*.uplugin", SearchOption.AllDirectories)
            .Where(x => x.Directory!.GetDirectories("Source").Length != 0)
            .Select(x => new ProjectDirInfo(Path.GetFileNameWithoutExtension(x.Name), x.DirectoryName!))
            .Where(x => x.GlueProjectName != "UnrealSharp")
            .ToImmutableArray();
        
        USharpBuildToolUtilities.CompileUSharpBuildTool();
        
        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            CSharpExporter.StartExport();
            
            stopwatch.Stop();
            Console.WriteLine($"Exporting completed in {stopwatch.Elapsed.Seconds} seconds.");
            
            FileExporter.CleanOldExportedFiles();
            GlueModuleFactory.CreateGlueProjects();
            
            if (GeneratorStatics.IsBuildingEditor && CSharpExporter.HasModifiedEngineGlue)
            {
                Console.WriteLine("Engine glue has been modified since the last build. Rebuilding UnrealSharp bindings...");
                DotNetUtilities.BuildSolution(Path.Combine(GeneratorStatics.ManagedPath, "UnrealSharp"));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred during the export process:");
            Console.WriteLine(ex);
        }
    }
}
