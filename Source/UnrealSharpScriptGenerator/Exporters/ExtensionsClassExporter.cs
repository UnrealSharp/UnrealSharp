using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public static class ExtensionsClassExporter
{
    public static void ExportExtensionsClass(UhtPackage package, List<ExtensionMethod> extensionMethods)
    {
        string className = $"{package.ShortName}Extensions";
        
        GeneratorStringBuilder stringBuilder = new();
        stringBuilder.GenerateTypeSkeleton(className);
        stringBuilder.DeclareType("static class", className, null, false);
        
        foreach (ExtensionMethod extensionMethod in extensionMethods)
        {
            FunctionExporter exporter = new(extensionMethod);
            exporter.ExportExtensionMethod(stringBuilder);
        }
        
        stringBuilder.CloseBrace();
        FileExporter.SaveTypeToDisk(package.ShortName, className, stringBuilder.ToString());
    }
}