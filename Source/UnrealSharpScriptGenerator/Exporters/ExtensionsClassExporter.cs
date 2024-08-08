using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public static class ExtensionsClassExporter
{
    public static void ExportExtensionsClass(UhtPackage package, List<ExtensionMethod> extensionMethods)
    {
        string typeNamespace = package.GetNamespace();
        string className = $"{package.ShortName}Extensions";
        
        GeneratorStringBuilder stringBuilder = new();
        stringBuilder.GenerateTypeSkeleton(typeNamespace);
        stringBuilder.DeclareType("static class", className, null, false);

        foreach (ExtensionMethod extensionMethod in extensionMethods)
        {
            FunctionExporter exporter = new FunctionExporter(extensionMethod);
            exporter.ExportExtensionMethod(stringBuilder);
        }
        
        stringBuilder.CloseBrace();
        
        string directory = FileExporter.GetDirectoryPath(package);
        FileExporter.SaveGlueToDisk(directory, className, stringBuilder.ToString(), package);
    }
}