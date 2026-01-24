using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.Exporters;

public static class ExtensionsClassExporter
{
    public static void ExportExtensionsClass(UhtPackage package, List<ExtensionMethod> extensionMethods)
    {
        Dictionary<UhtClass, List<ExtensionMethod>?> libraryToExtensionMethod = new();
        
        foreach (ExtensionMethod extensionMethod in extensionMethods)
        {
            UhtClass outerClass = (UhtClass) extensionMethod.Function.Outer!;
            
            if (!libraryToExtensionMethod.TryGetValue(outerClass, out List<ExtensionMethod>? libraryExtensions))
            {
                libraryExtensions = new List<ExtensionMethod>();
                libraryToExtensionMethod[outerClass] = libraryExtensions;
            }
            
            libraryExtensions!.Add(extensionMethod);
        }
        
        foreach (KeyValuePair<UhtClass, List<ExtensionMethod>?> pair in libraryToExtensionMethod)
        {
            UhtClass libraryClass = pair.Key;
            List<ExtensionMethod>? libraryExtensions = pair.Value;
            ExportLibrary(package, libraryClass, libraryExtensions!);
        }
    }

    public static void ExportLibrary(UhtPackage package, UhtClass libraryClass, List<ExtensionMethod> extensionMethods)
    {
        string className = $"{libraryClass.EngineName}_Extensions";
        
        GeneratorStringBuilder stringBuilder = new();
        stringBuilder.StartGlueFile(libraryClass);
        stringBuilder.DeclareType(package, "static class", className, null, false);
        
        foreach (ExtensionMethod extensionMethod in extensionMethods)
        {
            FunctionExporter exporter = new FunctionExporter(extensionMethod);
            exporter.Initialize(OverloadMode.AllowOverloads, EFunctionProtectionMode.UseUFunctionProtection, EBlueprintVisibility.Call);
            exporter.ExportExtensionMethodOverloads(stringBuilder);
            exporter.ExportExtensionMethod(stringBuilder);
        }
        
        stringBuilder.CloseBrace();
        stringBuilder.EndGlueFile(libraryClass);
        
        string directory = FileExporter.GetDirectoryPath(package);
        FileExporter.SaveGlueToDisk(package, directory, className, stringBuilder.ToString());
    }
}