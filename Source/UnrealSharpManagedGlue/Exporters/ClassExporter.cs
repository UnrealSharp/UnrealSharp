using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Attributes;
using UnrealSharpManagedGlue.PropertyTranslators;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;
using UnrealSharpManagedGlue.Tooltip;

namespace UnrealSharpManagedGlue.Exporters;

public static class ClassExporter
{
    public static void ExportClass(UhtClass classObj, bool isManualExport)
    {
        GeneratorStringBuilder stringBuilder = new();
        
        List<UhtFunction> exportedFunctions = new();
        List<UhtFunction> exportedOverrides = new();
        Dictionary<string, GetterSetterPair> exportedGetterSetters = new();
        Dictionary<string, GetterSetterPair> getSetOverrides = new();
        
        ScriptGeneratorUtilities.GetExportedFunctions(classObj, exportedFunctions, 
            exportedOverrides, 
            exportedGetterSetters, getSetOverrides);
        
        List<UhtProperty> exportedProperties = new List<UhtProperty>();
        Dictionary<UhtProperty, GetterSetterPair> getSetBackedProperties = new();
        ScriptGeneratorUtilities.GetExportedProperties(classObj, exportedProperties, getSetBackedProperties);
        
        List<UhtClass> interfaces = classObj.GetInterfaces();
        
        bool nullableEnabled = classObj.HasMetadata(UhtTypeUtilities.NullableEnable);
        stringBuilder.StartGlueFile(classObj, nullableEnabled: nullableEnabled);
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
            superClassName = "UnrealSharp.Core.UnrealSharpObject";
        }
        
        stringBuilder.DeclareType(classObj, "class", classObj.GetStructName(), superClassName, nativeInterfaces: interfaces);
        stringBuilder.AppendNativeTypePtr(classObj);
        
        // For manual exports we just want to generate attributes
        if (!isManualExport)
        { 
            StaticConstructorUtilities.ExportStaticConstructor(stringBuilder, classObj, 
                exportedProperties, 
                exportedFunctions, 
                exportedGetterSetters,
                getSetBackedProperties,
                exportedOverrides);
            
            HashSet<string> exportedPropertyNames = new();
            HashSet<string> exportedFunctionNames = new();
            ExportClassProperties(stringBuilder, exportedProperties, exportedPropertyNames);
            ExportGetSetProperties(stringBuilder, getSetBackedProperties, exportedPropertyNames, exportedFunctionNames);
            ExportCustomProperties(stringBuilder, exportedGetterSetters, exportedPropertyNames, exportedFunctionNames);
            
            ExportClassFunctions(classObj, stringBuilder, exportedFunctions, exportedFunctionNames);
            ExportGetSetOverrides(stringBuilder, getSetOverrides, exportedPropertyNames, exportedFunctionNames);
            ExportOverrides(stringBuilder, exportedOverrides, exportedFunctionNames);
            stringBuilder.AppendLine();
        }

        stringBuilder.CloseBrace();
        
        stringBuilder.EndGlueFile(classObj);
        
        FileExporter.SaveGlueToDisk(classObj, stringBuilder);
    }

    static void ExportClassProperties(GeneratorStringBuilder generatorStringBuilder, List<UhtProperty> exportedProperties, HashSet<string> exportedPropertyNames)
    {
        foreach (UhtProperty property in exportedProperties)
        {
            PropertyTranslator translator = property.GetTranslator()!;
            translator.ExportProperty(generatorStringBuilder, property);
            exportedPropertyNames.Add(property.SourceName);
        }
    }
    
    public static void ExportGetSetProperties(GeneratorStringBuilder builder, 
                                              Dictionary<UhtProperty, GetterSetterPair> getSetBackedProperties, 
                                              HashSet<string> exportedPropertyNames,
                                              HashSet<string> exportedFunctionNames)
    {
        Dictionary<UhtFunction, FunctionExporter> exportedGetterSetters = new();
        foreach (KeyValuePair<UhtProperty, GetterSetterPair> pair in getSetBackedProperties)
        {
            UhtProperty property = pair.Key;
            GetterSetterPair getterSetterPair = pair.Value;
            PropertyTranslator translator = property.GetTranslator()!;
            translator.ExportGetSetProperty(builder, getterSetterPair, property, exportedGetterSetters, exportedFunctionNames);
            
            exportedPropertyNames.Add(getterSetterPair.PropertyName);
            foreach (UhtFunction function in getterSetterPair.Accessors)
            {
                exportedFunctionNames.Add(function.SourceName);
            }
        }
    }

    private static void ExportGetSetOverrides(GeneratorStringBuilder builder, Dictionary<string, GetterSetterPair> getSetBackedProperties, 
                                              HashSet<string> exportedPropertyNames, HashSet<string> exportedFunctionNames)
    {
        foreach (KeyValuePair<string, GetterSetterPair> pair in getSetBackedProperties)
        {
            if (pair.Value.Property == null)
            {
                throw new InvalidDataException($"Property '{pair.Value.PropertyName}' does not have a UProperty");
            }
            
            UhtFunction firstAccessor = pair.Value.Accessors.First();
            UhtProperty firstProperty = pair.Value.Property;
            string propertyName = pair.Value.PropertyName;
            
            PropertyTranslator translator = firstProperty.GetTranslator()!;
            builder.TryAddWithEditor(firstAccessor);
            translator.ExportCustomProperty(builder, pair.Value, propertyName, firstProperty, 
                exportedPropertyNames.Contains(propertyName), exportedFunctionNames);
            builder.TryEndWithEditor(firstAccessor);
            
            exportedPropertyNames.Add(propertyName);
            foreach (UhtFunction function in pair.Value.Accessors)
            {
                exportedFunctionNames.Add(function.SourceName);
            }
        }
    }
    
    static void ExportOverrides(GeneratorStringBuilder builder, List<UhtFunction> exportedOverrides, HashSet<string> exportedFunctionNames)
    {
        foreach (UhtFunction function in exportedOverrides)
        {
            FunctionExporter.ExportOverridableFunction(builder, function, exportedFunctionNames);
            exportedFunctionNames.Add(function.SourceName);
        }
    }
    
    static void ExportClassFunctions(UhtClass owner, GeneratorStringBuilder builder, List<UhtFunction> exportedFunctions,
                                     HashSet<string> exportedFunctionNames)
    {
        bool isBlueprintFunctionLibrary = owner.IsChildOf(GeneratorStatics.BlueprintFunctionLibrary);
        foreach (UhtFunction function in exportedFunctions)
        {
            if (function.HasAllFlags(EFunctionFlags.Static) && isBlueprintFunctionLibrary)
            {
                FunctionExporter.TryAddExtensionMethod(function);
            }
            
            FunctionExporter.ExportFunction(builder, function, FunctionType.Normal, exportedFunctionNames);
            exportedFunctionNames.Add(function.SourceName);
        }
    }

    public static void ExportCustomProperties(GeneratorStringBuilder builder, 
                                              Dictionary<string, GetterSetterPair> exportedGetterSetters, 
                                              HashSet<string> exportedPropertyNames,
                                              HashSet<string> exportedFunctionNames)
    {
        foreach (KeyValuePair<string, GetterSetterPair> pair in exportedGetterSetters)
        {
            if (pair.Value.Property == null)
            {
                throw new InvalidDataException($"Property '{pair.Value.PropertyName}' does not have a UProperty");
            }
            
            UhtFunction firstAccessor = pair.Value.Accessors.First();
            UhtProperty firstProperty = pair.Value.Property;
            string propertyName = pair.Value.PropertyName;
            
            PropertyTranslator translator = firstProperty.GetTranslator()!;
            builder.TryAddWithEditor(firstAccessor);
            translator.ExportCustomProperty(builder, pair.Value, propertyName, firstProperty, exportedFunctionNames: exportedFunctionNames);
            builder.TryEndWithEditor(firstAccessor);
            
            exportedPropertyNames.Add(propertyName);
            foreach (UhtFunction function in pair.Value.Accessors)
            {
                exportedFunctionNames.Add(function.SourceName);
            }
        }
    }
}