using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.PropertyTranslators;

namespace UnrealSharpManagedGlue.Utilities;

internal static class StructDefaultValue
{
    public static bool TryScalar(UhtProperty property, string text, out string expression)
        => TryValue(property, new UnrealDefaultValueParser.Value(text), out expression);

    public static bool TryTranslate(UhtStructProperty property, string text, out string expression)
    {
        expression = "";
        try
        {
            return TryStruct(property, UnrealDefaultValueParser.Parse(text), out expression);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryStruct(UhtStructProperty property, UnrealDefaultValueParser.Value value, out string expression)
    {
        expression = "";
        PropertyTranslator? translator = property.GetTranslator();
        if (translator == null || value.Fields == null)
            return false;

        string type = translator.GetManagedType(property);
        if (value.Fields.Count == 0)
        {
            expression = $"new {type}()";
            return true;
        }
        if (property.ScriptStruct.HasMetadata("ReadOnly"))
            return false;

        // Positional syntax is a type-specific UHT convention, not reflected field order.
        string[]? positional = property.ScriptStruct.GetAttributeValue("DefaultValueFields")?.Split(',');
        bool named = value.Fields.All(field => field.Name != null);
        if (!named && (value.Fields.Any(field => field.Name != null) || positional?.Length != value.Fields.Count))
            return false;

        List<string> assignments = new();
        HashSet<string> members = new(StringComparer.Ordinal);
        for (int i = 0; i < value.Fields.Count; ++i)
        {
            UnrealDefaultValueParser.Field field = value.Fields[i];
            string name = field.Name ?? positional![i];
            UhtProperty? member = null;
            for (UhtStruct? owner = property.ScriptStruct; owner != null && member == null; owner = owner.Super)
                member = owner.Properties.FirstOrDefault(p => string.Equals(p.SourceName, name, StringComparison.OrdinalIgnoreCase));
            if (member == null || !member.CanExportProperty() || !string.IsNullOrEmpty(member.ArrayDimensions))
                return false;

            string managedName = property.ScriptStruct.GetAttributeValue("DefaultValueMember." + member.SourceName)
                ?? (property.ScriptStruct.IsStructBlittable() ? member.SourceName : member.GetPropertyName());
            if (!members.Add(managedName) || !TryValue(member, field.Value, out string inner))
                return false;
            assignments.Add($"{managedName} = {inner}");
        }
        expression = $"new {type} {{ {string.Join(", ", assignments)} }}";
        return true;
    }

    private static bool TryValue(UhtProperty property, UnrealDefaultValueParser.Value value, out string expression)
    {
        expression = "";
        if (property is UhtStructProperty nested)
            return TryStruct(nested, value, out expression);
        if (value.Fields != null)
            return false;

        string text = value.Text.Trim();
        PropertyTranslator translator = property.GetTranslator()!;
        UhtEnum? enumType = property is UhtEnumProperty e ? e.Enum : (property as UhtByteProperty)?.Enum;
        if (enumType != null)
        {
            int index = enumType.GetIndexByName(text);
            if (index < 0 || index >= enumType.EnumValues.Count)
                return false;
            expression = $"{translator.GetManagedType(property)}.{ScriptGeneratorUtilities.GetCleanEnumValueName(enumType, enumType.EnumValues[index])}";
            return true;
        }
        if (property is UhtBoolProperty)
        {
            if (!bool.TryParse(text, out bool boolean))
                return false;
            expression = boolean ? "true" : "false";
            return true;
        }
        if (property is UhtNumericProperty)
        {
            string type = translator.GetManagedType(property);
            if (type is "float" or "double")
            {
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) || !double.IsFinite(number))
                    return false;
                if (type == "float")
                {
                    float single = (float)number;
                    if (!float.IsFinite(single)) return false;
                    expression = single.ToString("R", CultureInfo.InvariantCulture) + "f";
                }
                else expression = number.ToString("R", CultureInfo.InvariantCulture) + "d";
                return true;
            }
            if (!decimal.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out decimal integer))
                return false;
            (decimal min, decimal max, string suffix) bounds = type switch
            {
                "sbyte" => (sbyte.MinValue, sbyte.MaxValue, ""),
                "byte" => (byte.MinValue, byte.MaxValue, ""),
                "short" => (short.MinValue, short.MaxValue, ""),
                "ushort" => (ushort.MinValue, ushort.MaxValue, ""),
                "int" => (int.MinValue, int.MaxValue, ""),
                "uint" => (uint.MinValue, uint.MaxValue, "U"),
                "long" => (long.MinValue, long.MaxValue, "L"),
                "ulong" => (ulong.MinValue, ulong.MaxValue, "UL"),
                _ => (1, 0, "")
            };
            if (integer < bounds.min || integer > bounds.max) return false;
            expression = integer.ToString(CultureInfo.InvariantCulture) + bounds.suffix;
            return true;
        }
        if (property is UhtStrProperty)
        {
            try
            {
                // Accept only a complete quoted literal; unsupported escape syntax must not leak into C#.
                string? literal = JsonSerializer.Deserialize<string>(text);
                if (literal == null) return false;
                expression = JsonSerializer.Serialize(literal);
                return true;
            }
            catch (JsonException) { return false; }
        }
        return false;
    }
}
