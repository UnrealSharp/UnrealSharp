using System;
using System.Diagnostics;
using System.IO;
using EpicGames.UHT.Tables;
using EpicGames.UHT.Utils;
using UnrealBuildTool;
using UnrealSharp.Shared;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

[UnrealHeaderTool]
public static class Program
{
    [UhtExporter(Name = "UnrealSharpCore", Description = "Exports C++ to C# code", Options = UhtExporterOptions.Default, ModuleName = "UnrealSharpCore")]
    private static void Main(IUhtExportFactory factory)
    {
        Console.WriteLine("Initializing C# exporter...");
        GeneratorStatics.Initialize(factory);
        USharpBuildToolUtilities.CompileUSharpBuildTool();
            
        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            CSharpExporter.StartExport();
            FileExporter.CleanOldExportedFiles();
            
            stopwatch.Stop();
            Console.WriteLine($"Exporting completed in {stopwatch.Elapsed.Seconds} seconds.");
            
            GlueModuleFactory.CreateGlueProjects();
            
            if (GeneratorStatics.BuildTarget == TargetType.Editor && CSharpExporter.HasModifiedEngineGlue)
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
