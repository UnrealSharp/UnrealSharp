using System.Collections.Generic;
using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public static class ClassExporter 
{
    public static void ExportClass(UhtClass classObj)
    {
        using BorrowStringBuilder borrower = new(StringBuilderCache.Big);
        StringBuilder stringBuilder = borrower.StringBuilder;

        string className = ScriptGeneratorUtilities.GetCleanTypeName(classObj);
        string moduleName = ScriptGeneratorUtilities.GetModuleName(classObj);

        List<UhtProperty> exportedProperties = new();
        ScriptGeneratorUtilities.GetExportedProperties(classObj, ref exportedProperties);
        
        List<UhtFunction> exportedFunctions = new();
        List<UhtFunction> exportedOverrides = new();
        ScriptGeneratorUtilities.GetExportedFunctions(classObj, ref exportedFunctions, ref exportedOverrides);

        List<UhtType> interfaces = new();
        ScriptGeneratorUtilities.GetInterfaces(classObj, ref interfaces);

        List<string> classDependencies = new();
        ScriptGeneratorUtilities.GatherDependencies(classObj, exportedFunctions, exportedOverrides, exportedProperties, interfaces, classDependencies);
        
        stringBuilder.DeclareDirectives(classDependencies);
        stringBuilder.GenerateTypeSkeleton(moduleName);
        
        string abstractModifier = classObj.ClassFlags.HasAnyFlags(EClassFlags.Abstract) ? "ClassFlags.Abstract" : "";
        stringBuilder.AppendLine($"[UClass({abstractModifier})]");
        
        string cleanSuperClass = ScriptGeneratorUtilities.GetCleanTypeName(classObj.SuperClass);
        stringBuilder.DeclareType("class", className, cleanSuperClass, true, interfaces);
        
        StaticConstructorUtilities.ExportStaticConstructor(stringBuilder, classObj, exportedProperties, exportedFunctions, exportedOverrides);
        ExportClassProperties(stringBuilder, exportedProperties);
        
        stringBuilder.CloseBrace();
        
        ScriptGeneratorUtilities.SaveExportedType(classObj, borrower);
    }

    static void ExportClassProperties(StringBuilder stringBuilder, List<UhtProperty> exportedProperties)
    {
        foreach (UhtProperty property in exportedProperties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property);
            translator.ExportProperty(stringBuilder, property);
        }
    }
}