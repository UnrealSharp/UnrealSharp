using System;
using System.Collections.Generic;
using EpicGames.UHT.Tokenizer;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Attributes;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;
using UnrealSharpManagedGlue.Tooltip;

namespace UnrealSharpManagedGlue.Exporters;

public static class EnumExporter
{
    public static void ExportEnum(UhtEnum enumObj)
    {
        GeneratorStringBuilder stringBuilder = new GeneratorStringBuilder();
        
        stringBuilder.StartGlueFile(enumObj);
        stringBuilder.AppendTooltip(enumObj);
        
        AttributeBuilder attributeBuilder = new AttributeBuilder(enumObj);
        attributeBuilder.Finish();
        
        stringBuilder.AppendLine(attributeBuilder.ToString());
        
        string underlyingType = UnderlyingEnumTypeToString(enumObj.UnderlyingType);
        stringBuilder.DeclareType(enumObj, "enum", enumObj.GetStructName(), underlyingType, isPartial: false);
        
        Dictionary<string, string> initializers = GetUnparsedInitializers(enumObj);
        int enumValuesCount = enumObj.EnumValues.Count;
        for (int i = 0; i < enumValuesCount; i++)
        {
            UhtEnumValue enumValue = enumObj.EnumValues[i];

            string toolTip = enumObj.GetMetadata("Tooltip", i);
            stringBuilder.AppendTooltip(toolTip);
            
            string cleanValueName = ScriptGeneratorUtilities.GetCleanEnumValueName(enumObj, enumValue);
            string value = enumValue.Value != -1 ? $" = {enumValue.Value}," :
                initializers.TryGetValue(cleanValueName, out string? initializer) ? $" = {initializer}," : ",";
            
            stringBuilder.AppendLine($"{cleanValueName}{value}");
        }
        
        stringBuilder.CloseBrace();
        stringBuilder.EndGlueFile(enumObj);
        
        FileExporter.SaveGlueToDisk(enumObj, stringBuilder);
    }

    private static Dictionary<string, string> GetUnparsedInitializers(UhtEnum enumObj)
    {
        Dictionary<string, string> initializers = new(StringComparer.Ordinal);
        if (!enumObj.EnumValues.Exists(value => value.Value == -1))
        {
            return initializers;
        }

        // UHT stores -1 for initializers it cannot evaluate, including aliases. Omitting
        // those initializers in C# changes their values and can overflow the enum's type.
        UhtTokenBufferReader reader = new(enumObj, enumObj.HeaderFile.Data.Memory);
        while (!reader.IsEOF)
        {
            UhtToken token = reader.GetToken();
            if (token.InputLine >= enumObj.LineNumber && token.IsIdentifier(enumObj.SourceName))
            {
                break;
            }
        }

        reader.SkipUntil('{').Require('{');
        if (enumObj.CppForm == UhtEnumCppForm.Namespaced)
        {
            reader.SkipUntil('{').Require('{');
        }

        Dictionary<string, string> previousMembers = new(StringComparer.Ordinal);
        Dictionary<string, UhtEnumValue> members = new(StringComparer.Ordinal);
        foreach (UhtEnumValue member in enumObj.EnumValues)
        {
#if UE_5_7_OR_LATER
            members.Add(enumObj.GetShortEnumName(member.Name), member);
#else
            members.Add(GetShortEnumName(enumObj, member.Name), member);
#endif
        }

        while (!reader.IsEOF && !reader.PeekToken().IsSymbol('}'))
        {
            UhtToken name = reader.GetToken();
            if (!name.IsIdentifier() || !members.TryGetValue(name.Value.ToString(), out UhtEnumValue member))
            {
                continue;
            }

            reader.OptionalAttributes(false);
            List<UhtToken> expression = new();
            if (reader.TryOptional('='))
            {
                while (!reader.IsEOF && !reader.PeekToken().IsSymbol(',') &&
                       !reader.PeekToken().IsSymbol('}') && !reader.PeekToken().IsIdentifier("UMETA"))
                {
                    expression.Add(reader.GetToken());
                }
            }

            string memberName = ScriptGeneratorUtilities.GetCleanEnumValueName(enumObj, member);
            if (member.Value == -1 && TryGetInitializer(enumObj, expression, previousMembers, out string initializer))
            {
                initializers.Add(memberName, initializer);
            }

            previousMembers.TryAdd(name.Value.ToString(), memberName);
            if (reader.TryOptional("UMETA"))
            {
                reader.SkipBrackets('(', ')', 0);
            }
            reader.TryOptional(',');
        }

        return initializers;
    }

#if !UE_5_7_OR_LATER
    // UE 5.6 does not expose UhtEnum.GetShortEnumName.
    private static string GetShortEnumName(UhtEnum enumObj, string name)
    {
        switch (enumObj.CppForm)
        {
            case UhtEnumCppForm.Namespaced:
            case UhtEnumCppForm.EnumClass:
                int lastColons = name.LastIndexOf("::", StringComparison.Ordinal);
                return lastColons == -1 ? name : name[(lastColons + 2)..];
            case UhtEnumCppForm.Regular:
                return name;
            default:
                throw new ArgumentOutOfRangeException(nameof(enumObj.CppForm), enumObj.CppForm, null);
        }
    }
#endif

    private static bool TryGetInitializer(UhtEnum enumObj, List<UhtToken> expression,
        Dictionary<string, string> previousMembers, out string initializer)
    {
        initializer = "";
        if (expression.Count == 1 && expression[0].GetConstLong(out long literal))
        {
            initializer = literal.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
        if (expression.Count == 2 && expression[0].IsSymbol('-') &&
            expression[1].GetConstLong(out long magnitude) && magnitude == 1)
        {
            initializer = "-1";
            return true;
        }

        // Accept only a reference to an earlier member of this same native enum.
        // Other C++ expressions still use the existing fallback, rather than guessing.
        string reference = "";
        for (int i = 0; i < expression.Count; ++i)
        {
            UhtToken token = expression[i];
            if (i % 2 == 0 ? !token.IsIdentifier() : !token.IsSymbol("::"))
            {
                return false;
            }
            reference += token.Value.ToString();
        }

        int qualifierIndex = reference.LastIndexOf("::", StringComparison.Ordinal);
        if (qualifierIndex >= 0)
        {
            string qualifier = reference[..qualifierIndex];
            if (qualifier != enumObj.SourceName && qualifier != enumObj.CppType)
            {
                return false;
            }
            reference = reference[(qualifierIndex + 2)..];
        }

        if (previousMembers.TryGetValue(reference, out string? memberName))
        {
            initializer = memberName;
            return true;
        }
        return false;
    }

    private static string UnderlyingEnumTypeToString(UhtEnumUnderlyingType underlyingType)
    {
        return underlyingType switch
        {
            UhtEnumUnderlyingType.Unspecified => "",
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
