using System;
using System.Collections.Generic;

namespace UnrealSharpManagedGlue.Utilities;

/// <summary>Parses the fields and nested tuples used by CPP_Default metadata.</summary>
internal static class UnrealDefaultValueParser
{
    internal sealed record Value(string Text, IReadOnlyList<Field>? Fields = null);
    internal sealed record Field(string? Name, Value Value);

    // Corresponds to UScriptStruct::ImportText (CoreUObject/Private/UObject/Class.cpp):
    // read each property, then advance past its comma. Keep legacy parenthesis-free tuples.
    public static Value Parse(string text)
    {
        string contents = text.Trim();
        if (contents.StartsWith("(") && contents.EndsWith(")"))
        {
            contents = contents.Substring(1, contents.Length - 2);
        }

        List<Field> fields = new();
        int position = 0;
        while (position < contents.Length)
        {
            Field? field = ImportSingleProperty(contents, ref position);
            if (field != null)
            {
                fields.Add(field);
            }

            if (position < contents.Length)
            {
                if (contents[position] != ',')
                {
                    throw new FormatException($"Unexpected ')' in '{text}'.");
                }
                ++position;
            }
        }
        return new Value(text, fields);
    }

    // Corresponds to FProperty::ImportSingleProperty (CoreUObject/Private/UObject/Property.cpp):
    // read the name and '=', then read the value. Unnamed values retain the tuple behavior.
    private static Field? ImportSingleProperty(string text, ref int position)
    {
        string value = ReadValue(text, ref position, stopAtEquals: true);
        string? name = null;
        if (position < text.Length && text[position] == '=')
        {
            name = value.Trim('"', '\'');
            ++position;
            value = ReadValue(text, ref position);
            if (name.Length == 0 || value.Length == 0)
            {
                throw new FormatException($"Expected a field name and value around '=' in '{text}'.");
            }
        }

        return value.Length == 0 ? null
            : new Field(name, value.StartsWith("(") ? Parse(value) : new Value(value));
    }

    // Corresponds to the SubCount/quoted-text scan in UScriptStruct::ImportText (Class.cpp).
    // Read up to a top-level delimiter, preserving the text without decoding escapes.
    private static string ReadValue(string text, ref int position, bool stopAtEquals = false)
    {
        int start = position;
        int subCount = 0;
        while (position < text.Length &&
               (subCount > 0 || text[position] != ')') &&
               (subCount > 0 || text[position] != ',') &&
               (!stopAtEquals || subCount > 0 || text[position] != '='))
        {
            char character = text[position];
            if (character is '"' or '\'')
            {
                char quote = character;
                ++position;
                while (position < text.Length && text[position] != quote)
                {
                    if (text[position] == '\\' && position + 1 < text.Length)
                    {
                        ++position;
                    }
                    ++position;
                }
                if (position == text.Length)
                {
                    throw new FormatException($"Bad quoted string in '{text}'.");
                }
            }
            else if (character == '(')
            {
                ++subCount;
            }
            else if (character == ')')
            {
                --subCount;
            }
            ++position;
        }
        if (subCount > 0)
        {
            throw new FormatException($"Missing closing ')' in '{text}'.");
        }
        return text.Substring(start, position - start).Trim();
    }
}
