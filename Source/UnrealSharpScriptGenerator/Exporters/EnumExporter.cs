using System;
using System.IO;
using System.Text;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;

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
        
        foreach (UhtEnumValue enumValue in enumObj.EnumValues)
        {
            stringBuilder.AppendLine($"     {GetCleanEnumValueName(enumObj, enumValue)} = {enumValue.Value},");
        }
        
        stringBuilder.CloseBrace();
        ScriptGeneratorUtilities.SaveExportedType(enumObj, borrower);
    }
    
    private static string GetCleanEnumValueName(UhtEnum enumObj, UhtEnumValue enumValue)
    {
        if (enumObj.CppForm == UhtEnumCppForm.Regular)
        {
            return enumValue.Name;
        }
        
        int delimiterIndex = enumValue.Name.IndexOf("::", StringComparison.Ordinal);
        return delimiterIndex < 0 ? enumValue.Name : enumValue.Name.Substring(delimiterIndex + 2);
    }
}