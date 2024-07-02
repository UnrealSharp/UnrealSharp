using System.Collections.Generic;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public static class ClassExporter 
{
    public static void ExportClass(UhtClass classObj)
    {
        GeneratorStringBuilder stringBuilder = new();
        
        string typeNameSpace = ScriptGeneratorUtilities.GetNamespace(classObj);

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
        stringBuilder.GenerateTypeSkeleton(typeNameSpace);
        
        string abstractModifier = classObj.ClassFlags.HasAnyFlags(EClassFlags.Abstract) ? "ClassFlags.Abstract" : "";
        stringBuilder.AppendLine($"[UClass({abstractModifier})]");

        string superClassName;
        if (classObj.SuperClass != null)
        {
            superClassName = ScriptGeneratorUtilities.GetFullManagedName(classObj.SuperClass);
        }
        else
        {
            superClassName = "UnrealSharpObject";
        }
        
        stringBuilder.DeclareType("class", classObj.EngineName, superClassName, true, interfaces);
        
        StaticConstructorUtilities.ExportStaticConstructor(stringBuilder, classObj, exportedProperties, exportedFunctions, exportedOverrides);
        ExportClassProperties(stringBuilder, exportedProperties);
        
        stringBuilder.CloseBrace();
        
        ScriptGeneratorUtilities.SaveExportedType(classObj, stringBuilder);
    }

    static void ExportClassProperties(GeneratorStringBuilder generatorStringBuilder, List<UhtProperty> exportedProperties)
    {
        foreach (UhtProperty property in exportedProperties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property);
            translator.ExportProperty(generatorStringBuilder, property);
        }
    }
}