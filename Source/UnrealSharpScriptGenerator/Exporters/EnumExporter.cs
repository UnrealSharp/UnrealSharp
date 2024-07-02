using System;
using System.IO;
using System.Text;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public static class EnumExporter
{
    public static void ExportEnum(UhtEnum enumObj)
    {
        GeneratorStringBuilder stringBuilder = new GeneratorStringBuilder();
        
        string moduleName = ScriptGeneratorUtilities.GetNamespace(enumObj);
        
        stringBuilder.GenerateTypeSkeleton(moduleName);
        stringBuilder.AppendLine("[UEnum]");
        
        string underlyingType = UnderlyingTypeToString(enumObj.UnderlyingType);
        stringBuilder.DeclareType("enum", enumObj.EngineName, underlyingType, isPartial: false);
        
        stringBuilder.Indent();
        foreach (UhtEnumValue enumValue in enumObj.EnumValues)
        {
            string cleanValueName = ScriptGeneratorUtilities.GetCleanEnumValueName(enumObj, enumValue);
            stringBuilder.AppendLine($"{cleanValueName} = {enumValue.Value},");
        }
        stringBuilder.UnIndent();
        
        stringBuilder.CloseBrace();
        ScriptGeneratorUtilities.SaveExportedType(enumObj, stringBuilder);
    }
    
    public static string UnderlyingTypeToString(UhtEnumUnderlyingType underlyingType)
    {
        return underlyingType switch
        {
            UhtEnumUnderlyingType.Uint8 => "byte",
            UhtEnumUnderlyingType.Int8 => "sbyte",
            UhtEnumUnderlyingType.Int16 => "short",
            UhtEnumUnderlyingType.Int => "int",
            UhtEnumUnderlyingType.Int32 => "int",
            UhtEnumUnderlyingType.Int64 => "long",
            UhtEnumUnderlyingType.Uint16 => "ushort",
            UhtEnumUnderlyingType.Uint32 => "uint",
            UhtEnumUnderlyingType.Uint64 => "ulong",
            _ => throw new ArgumentOutOfRangeException(nameof(underlyingType), underlyingType, null)
        };
        
    }
}