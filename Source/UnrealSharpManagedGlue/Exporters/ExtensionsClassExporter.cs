using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public static class ExtensionsClassExporter
{
    public static void ExportExtensionsClass(UhtPackage package, List<ExtensionMethod> extensionMethods)
    {
        Dictionary<UhtType, List<ExtensionMethod>?> libraryToExtensionMethod = new();
        
        foreach (ExtensionMethod extensionMethod in extensionMethods)
        {
            UhtType outerClass = extensionMethod.Function.Outer!;
            
            if (!libraryToExtensionMethod.TryGetValue(outerClass, out List<ExtensionMethod>? libraryExtensions))
            {
                libraryExtensions = new List<ExtensionMethod>();
                libraryToExtensionMethod[outerClass] = libraryExtensions;
            }
            
            libraryExtensions!.Add(extensionMethod);
        }
        
        foreach (KeyValuePair<UhtType, List<ExtensionMethod>?> pair in libraryToExtensionMethod)
        {
            UhtType libraryClass = pair.Key;
            List<ExtensionMethod>? libraryExtensions = pair.Value;
            ExportLibrary(package, libraryClass, libraryExtensions!);
        }
    }

    public static void ExportLibrary(UhtPackage package, UhtType libraryClass, List<ExtensionMethod> extensionMethods)
    {
        string typeNamespace = package.GetNamespace();
        string className = $"{libraryClass.EngineName}_Extensions";
        
        GeneratorStringBuilder stringBuilder = new();
        stringBuilder.GenerateTypeSkeleton(typeNamespace);
        stringBuilder.DeclareType(package, "static class", className, null, false);
        
        foreach (ExtensionMethod extensionMethod in extensionMethods)
        {
            FunctionExporter exporter = new FunctionExporter(extensionMethod);
            exporter.Initialize(OverloadMode.AllowOverloads, EFunctionProtectionMode.UseUFunctionProtection, EBlueprintVisibility.Call);
            exporter.ExportExtensionMethodOverloads(stringBuilder);
            exporter.ExportExtensionMethod(stringBuilder);
        }
        
        stringBuilder.CloseBrace();
        
        string directory = FileExporter.GetDirectoryPath(package);
        FileExporter.SaveGlueToDisk(package, directory, className, stringBuilder.ToString());
    }
}