using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnrealSharp.Shared;

namespace UnrealSharpManagedGlue.Utilities;

public static class ExporterValidator
{
    static bool IsExporterStale()
    {
        string executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
        DateTime executingAssemblyLastWriteTime = File.GetLastWriteTimeUtc(executingAssemblyPath);

        string generatedCodeDirectory = GeneratorStatics.PluginModule.OutputDirectory;
        string timestampFilePath = Path.Combine(generatedCodeDirectory, "Timestamp");

        if (!File.Exists(timestampFilePath) || !Directory.Exists(GeneratorStatics.BindingsProjectDirectory))
        {
            return true;
        }

        DateTime savedTimestampUtc = File.GetLastWriteTimeUtc(timestampFilePath);
        return executingAssemblyLastWriteTime > savedTimestampUtc;
    }

    public static void ValidateExporter()
    {
        ConsoleUtilities.Log("Validating exporter...");

        if (!IsExporterStale())
        {
            PackageHeadersTracker.DeserializeModuleHeaders();
            return;
        }

        ConsoleUtilities.Log("Exporter is stale. Re-exporting all bindings...");
        CleanModules();
    }

    private static void CleanModules()
    {
        IEnumerable<ModuleInfo> modules = ModuleUtilities.Modules;
        foreach (ModuleInfo module in modules)
        {
            FileExporter.CleanGeneratedFolder(module.GlueBaseDirectory);

            if (module.IsPartOfEngine || !File.Exists(module.CsProjPath))
            {
                continue;
            }

            File.Delete(module.CsProjPath);
        }
    }
}