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
        using BorrowStringBuilder borrower = new(StringBuilderCache.Big);
        StringBuilder stringBuilder = borrower.StringBuilder;
        
        string moduleName = ScriptGeneratorUtilities.GetModuleName(enumObj);
        
        stringBuilder.GenerateTypeSkeleton(moduleName);
        borrower.StringBuilder.AppendLine("[UEnum]");
        
        string underlyingType = enumObj.UnderlyingType.ToString().ToLower();
        stringBuilder.DeclareType("enum", enumObj.EngineName, underlyingType, isPartial: false);
        
        stringBuilder.Indent();
        foreach (UhtEnumValue enumValue in enumObj.EnumValues)
        {
            stringBuilder.AppendLine($"{ScriptGeneratorUtilities.GetCleanEnumValueName(enumObj, enumValue)} = {enumValue.Value},");
        }
        stringBuilder.UnIndent();
        
        stringBuilder.CloseBrace();
        ScriptGeneratorUtilities.SaveExportedType(enumObj, borrower);
    }
    
}