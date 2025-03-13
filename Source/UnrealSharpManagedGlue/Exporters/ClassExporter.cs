using System.Collections.Generic;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Tooltip;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public static class ClassExporter
{
    public static void ExportClass(UhtClass classObj, bool isManualExport)
    {
        GeneratorStringBuilder stringBuilder = new();

        string typeNameSpace = classObj.GetNamespace();
        
        List<UhtFunction> exportedFunctions = new();
        List<UhtFunction> exportedOverrides = new();
        Dictionary<string, GetterSetterPair> exportedGetterSetters = new();
        
        ScriptGeneratorUtilities.GetExportedFunctions(classObj, exportedFunctions, 
            exportedOverrides, 
            exportedGetterSetters);
        
        List<UhtProperty> exportedProperties = new List<UhtProperty>();
        Dictionary<UhtProperty, GetterSetterPair> getSetBackedProperties = new();
        ScriptGeneratorUtilities.GetExportedProperties(classObj, exportedProperties, getSetBackedProperties);
        
        List<UhtClass> interfaces = classObj.GetInterfaces();
        
        stringBuilder.GenerateTypeSkeleton(typeNameSpace);
        stringBuilder.AppendTooltip(classObj);
        
        AttributeBuilder attributeBuilder = new AttributeBuilder(classObj);
        if (classObj.ClassFlags.HasAnyFlags(EClassFlags.Abstract))
        {
            attributeBuilder.AddArgument("ClassFlags.Abstract");
        }
        attributeBuilder.AddGeneratedTypeAttribute(classObj);
        attributeBuilder.Finish();
        stringBuilder.AppendLine(attributeBuilder.ToString());

        string superClassName;
        if (classObj.SuperClass != null)
        {
            superClassName = classObj.SuperClass.GetFullManagedName();
        }
        else
        {
            superClassName = "UnrealSharpObject";
        }
        
        stringBuilder.DeclareType(classObj, "class", classObj.GetStructName(), superClassName, true, interfaces);
        
        // For manual exports we just want to generate attributes
        if (!isManualExport)
        { 
            StaticConstructorUtilities.ExportStaticConstructor(stringBuilder, classObj, 
                exportedProperties, 
                exportedFunctions, 
                exportedGetterSetters,
                getSetBackedProperties,
                exportedOverrides);
            
            ExportClassProperties(stringBuilder, exportedProperties);
            ExportGetSetProperties(stringBuilder, getSetBackedProperties);
            ExportCustomProperties(stringBuilder, exportedGetterSetters);
            
            ExportClassFunctions(classObj, stringBuilder, exportedFunctions);
            ExportOverrides(stringBuilder, exportedOverrides);
            stringBuilder.AppendLine();
        }

        stringBuilder.CloseBrace();
        
        FileExporter.SaveGlueToDisk(classObj, stringBuilder);
    }

    static void ExportClassProperties(GeneratorStringBuilder generatorStringBuilder, List<UhtProperty> exportedProperties)
    {
        foreach (UhtProperty property in exportedProperties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property)!;
            translator.ExportProperty(generatorStringBuilder, property);
        }
    }
    
    static void ExportGetSetProperties(GeneratorStringBuilder builder, Dictionary<UhtProperty, GetterSetterPair> getSetBackedProperties)
    {
        Dictionary<UhtFunction, FunctionExporter> exportedGetterSetters = new();
        foreach (KeyValuePair<UhtProperty, GetterSetterPair> pair in getSetBackedProperties)
        {
            UhtProperty property = pair.Key;
            GetterSetterPair getterSetterPair = pair.Value;
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property)!;
            translator.ExportGetSetProperty(builder, getterSetterPair, property, exportedGetterSetters);
        }
    }
    
    static void ExportOverrides(GeneratorStringBuilder builder, List<UhtFunction> exportedOverrides)
    {
        foreach (UhtFunction function in exportedOverrides)
        {
            FunctionExporter.ExportOverridableFunction(builder, function);
        }
    }
    
    static void ExportClassFunctions(UhtClass owner, GeneratorStringBuilder builder, List<UhtFunction> exportedFunctions)
    {
        bool isBlueprintFunctionLibrary = owner.IsChildOf(Program.BlueprintFunctionLibrary);
        foreach (UhtFunction function in exportedFunctions)
        {
            if (function.HasAllFlags(EFunctionFlags.Static) && isBlueprintFunctionLibrary)
            {
                FunctionExporter.TryAddExtensionMethod(function);
            }
            
            FunctionExporter.ExportFunction(builder, function, FunctionType.Normal);
        }
    }

    static void ExportCustomProperties(GeneratorStringBuilder builder, Dictionary<string, GetterSetterPair> exportedGetterSetters)
    {
        foreach (KeyValuePair<string, GetterSetterPair> pair in exportedGetterSetters)
        {
            UhtFunction firstAccessor = pair.Value.Accessors.First();
            UhtProperty firstProperty = pair.Value.Property;
            string propertyName = pair.Value.PropertyName;
            
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(firstProperty)!;
            builder.TryAddWithEditor(firstAccessor);
            translator.ExportCustomProperty(builder, pair.Value, propertyName, firstProperty);
            builder.TryEndWithEditor(firstAccessor);
        }
    }
}