using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;
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
            if (propertyNames.Contains(property.GetScriptName()))
            {
                exportedProperties.RemoveAt(i);
                i--;
            }
            else
            {
                propertyNames.Add(property.GetScriptName());
            }
        }
        
        string typeNameSpace = ScriptGeneratorUtilities.GetNamespace(structObj);
        stringBuilder.GenerateTypeSkeleton(typeNameSpace);
        
        bool isBlittable = structObj.IsStructBlittable();
        
        stringBuilder.AppendLine($"[UStruct({(isBlittable ? "IsBlittable = true" : "")})]");
        stringBuilder.DeclareType("struct", structObj.EngineName);
        
        ExportStructProperties(stringBuilder, exportedProperties, isBlittable);

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
        
        FileExporter.SaveTypeToDisk(structObj, stringBuilder);
    }
    
    public static void ExportStructProperties(GeneratorStringBuilder stringBuilder, List<UhtProperty> exportedProperties, bool suppressOffsets)
    {
        foreach (UhtProperty property in exportedProperties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property);
            translator.ExportMirrorProperty(stringBuilder, property, suppressOffsets);
        }
    }

    public static void ExportStructMarshaller(GeneratorStringBuilder builder, UhtScriptStruct structObj)
    {
        string structName = structObj.EngineName;
        
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
        builder.AppendLine($"public {structObj.EngineName}(IntPtr InNativeStruct)");
        builder.OpenBrace();
        builder.BeginUnsafeBlock();
        
        foreach (UhtProperty property in properties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property);
            string scriptName = property.GetScriptName();
            string assignmentOrReturn = $"{scriptName} =";
            string offsetName = $"{scriptName}_Offset";
            translator.ExportFromNative(builder, property, scriptName, assignmentOrReturn, "InNativeStruct", offsetName, false, false);
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
            string scriptName = property.GetScriptName();
            string offsetName = $"{scriptName}_Offset";
            translator.ExportToNative(builder, property, scriptName, "buffer", offsetName, scriptName);
        }
        
        builder.EndUnsafeBlock();
        builder.CloseBrace();
    }
}