using System;
using UnrealSharp.Shared;
using UnrealSharpManagedGlue.Exporters;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public static class GlueGenerator
{
    public static void GenerateBindings()
    {
        ExporterValidator.ValidateExporter();
        
        ConsoleUtilities.Log("Generating C# bindings...");
        PackageExporter.ExportPackages();
        PreprocessorExporter.ExportBuildDefines();
        FunctionExporter.BindExtensionMethods();
        AutocastExporter.BindAutocasts();
        
        PackageHeadersTracker.SerializeModuleData();
    }
}