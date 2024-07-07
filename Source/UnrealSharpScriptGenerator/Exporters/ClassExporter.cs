using System.Collections.Generic;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public static class ClassExporter
{
    private static Dictionary<string, ExtensionMethod> _overloads = new();
    
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
        ExportClassFunctions(stringBuilder, exportedFunctions);
        ExportOverrides(stringBuilder, exportedOverrides);
        
        stringBuilder.AppendLine();
        stringBuilder.CloseBrace();
        
        FileExporter.SaveTypeToDisk(classObj, stringBuilder);
    }

    static void ExportClassProperties(GeneratorStringBuilder generatorStringBuilder, List<UhtProperty> exportedProperties)
    {
        foreach (UhtProperty property in exportedProperties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property);
            translator.ExportProperty(generatorStringBuilder, property);
        }
    }
    
    static void ExportOverrides(GeneratorStringBuilder builder, List<UhtFunction> exportedOverrides)
    {
        foreach (UhtFunction function in exportedOverrides)
        {
            FunctionExporter.ExportOverridableFunction(builder, function);
        }
    }
    
    static void ExportClassFunctions(GeneratorStringBuilder builder, List<UhtFunction> exportedFunctions)
    {
        foreach (UhtFunction function in exportedFunctions)
        {
            FunctionType functionType = FunctionType.Normal;

            if (function.HasAllFlags(EFunctionFlags.Static) && function.Outer.EngineClassName == "UBlueprintFunctionLibrary")
            {
                ExtensionMethod? extensionMethod = GetExtensionMethodInfo(function);
                if (extensionMethod == null)
                {
                    continue;
                }
                
                string moduleName = ScriptGeneratorUtilities.GetModuleName(function.Outer);
                _overloads.Add(moduleName, extensionMethod.Value);
            }
            
            FunctionExporter.ExportFunction(builder, function, functionType);
        }
    }

    static ExtensionMethod? GetExtensionMethodInfo(UhtFunction function)
    {
        if (!function.HasMetadata("ExtensionMethod") || function.Children.Count == 0)
        {
            return null;
        }
        
        if (function.Children[0] is not UhtObjectPropertyBase selfProperty)
        {
            return null;
        }
        
        return new ExtensionMethod
        {
            Class = selfProperty.Class,
            Function = function,
            SelfParameter = selfProperty,
        };
    }
}