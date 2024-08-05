using System;
using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Tooltip;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public static class StructExporter
{
    public static void ExportStruct(UhtScriptStruct structObj)
    {
        GeneratorStringBuilder stringBuilder = new();
        List<UhtProperty> exportedProperties = new();
        if (structObj.SuperStruct != null)
        {
            ScriptGeneratorUtilities.GetExportedProperties(structObj.SuperStruct, ref exportedProperties);
        }
        ScriptGeneratorUtilities.GetExportedProperties(structObj, ref exportedProperties);
        
        // Check there are not properties with the same name, remove otherwise
        List<string> propertyNames = new();
        for (int i = 0; i < exportedProperties.Count; i++)
        {
            UhtProperty property = exportedProperties[i];
            string scriptName = property.GetParameterName();
            if (propertyNames.Contains(scriptName))
            {
                exportedProperties.RemoveAt(i);
                i--;
            }
            else
            {
                propertyNames.Add(scriptName);
            }
        }
        
        string typeNameSpace = structObj.GetNamespace();
        stringBuilder.GenerateTypeSkeleton(typeNameSpace);
        
        bool isBlittable = structObj.IsStructBlittable();
        
        stringBuilder.AppendTooltip(structObj);
        
        AttributeBuilder attributeBuilder = AttributeBuilder.CreateAttributeBuilder(structObj);
        if (isBlittable)
        {
            attributeBuilder.AddIsBlittableAttribute();
        }
        attributeBuilder.AddGeneratedTypeAttribute(structObj);
        attributeBuilder.Finish();
        stringBuilder.AppendLine(attributeBuilder.ToString());
        
        stringBuilder.DeclareType("struct", structObj.GetStructName());
        
        List<string> reservedNames = GetReservedNames(exportedProperties);
        
        ExportStructProperties(stringBuilder, exportedProperties, isBlittable, reservedNames);

        if (!isBlittable)
        {
            stringBuilder.AppendLine();
            StaticConstructorUtilities.ExportStaticConstructor(stringBuilder, structObj, exportedProperties, new List<UhtFunction>(), new List<UhtFunction>());
            stringBuilder.AppendLine();
            ExportMirrorStructMarshalling(stringBuilder, structObj, exportedProperties);
        }
        
        stringBuilder.CloseBrace();
        
        if (!isBlittable)
        {
            ExportStructMarshaller(stringBuilder, structObj);
        }
        
        FileExporter.SaveGlueToDisk(structObj, stringBuilder);
    }
    
    public static void ExportStructProperties(GeneratorStringBuilder stringBuilder, List<UhtProperty> exportedProperties, bool suppressOffsets, List<string> reservedNames)
    {
        foreach (UhtProperty property in exportedProperties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property)!;
            translator.ExportMirrorProperty(stringBuilder, property, suppressOffsets, reservedNames);
        }
    }
    
    public static List<string> GetReservedNames(List<UhtProperty> properties)
    {
        List<string> reservedNames = new();
        foreach (UhtProperty property in properties)
        {
            if (reservedNames.Contains(property.SourceName))
            {
                continue;
            }
            reservedNames.Add(property.SourceName);
        }
        return reservedNames;
    }

    public static void ExportStructMarshaller(GeneratorStringBuilder builder, UhtScriptStruct structObj)
    {
        string structName = structObj.GetStructName();
        
        builder.AppendLine();
        builder.AppendLine($"public static class {structName}Marshaller");
        builder.OpenBrace();
        
        builder.AppendLine($"public static {structName} FromNative(IntPtr nativeBuffer, int arrayIndex)");
        builder.OpenBrace();
        builder.AppendLine($"return new {structName}(nativeBuffer + (arrayIndex * GetNativeDataSize()));");
        builder.CloseBrace();
        
        builder.AppendLine();
        builder.AppendLine($"public static void ToNative(IntPtr nativeBuffer, int arrayIndex, {structName} obj)");
        builder.OpenBrace();
        builder.AppendLine($"obj.ToNative(nativeBuffer + (arrayIndex * GetNativeDataSize()));");
        builder.CloseBrace();

        builder.AppendLine();
        builder.AppendLine($"public static int GetNativeDataSize()");
        builder.OpenBrace();
        builder.AppendLine($"return {structName}.NativeDataSize;");
        builder.CloseBrace();
        builder.CloseBrace();
    }

    public static void ExportMirrorStructMarshalling(GeneratorStringBuilder builder, UhtScriptStruct structObj, List<UhtProperty> properties)
    {
        builder.AppendLine();
        builder.AppendLine($"public {structObj.GetStructName()}(IntPtr InNativeStruct)");
        builder.OpenBrace();
        builder.BeginUnsafeBlock();
        
        foreach (UhtProperty property in properties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property)!;
            string scriptName = property.GetPropertyName();
            string assignmentOrReturn = $"{scriptName} =";
            string offsetName = $"{property.SourceName}_Offset";
            builder.TryAddWithEditor(property);
            translator.ExportFromNative(builder, property, property.SourceName, assignmentOrReturn, "InNativeStruct", offsetName, false, false);
            builder.TryEndWithEditor(property);
        }
        
        builder.EndUnsafeBlock();
        builder.CloseBrace();
        
        builder.AppendLine();
        builder.AppendLine("public void ToNative(IntPtr buffer)");
        builder.OpenBrace();
        builder.BeginUnsafeBlock();
        
        foreach (UhtProperty property in properties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property)!;
            string scriptName = property.GetPropertyName();
            string offsetName = $"{property.SourceName}_Offset";
            builder.TryAddWithEditor(property);
            translator.ExportToNative(builder, property, property.SourceName, "buffer", offsetName, scriptName);
            builder.TryEndWithEditor(property);
        }
        
        builder.EndUnsafeBlock();
        builder.CloseBrace();
    }
}