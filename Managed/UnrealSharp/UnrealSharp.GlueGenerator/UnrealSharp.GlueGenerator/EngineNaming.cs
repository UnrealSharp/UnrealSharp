using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator;

public static class EngineNaming
{
    public const string DelegateSignatureSuffix = "__DelegateSignature";
    public const string GeneratedTypeAttributeName = "GeneratedTypeAttribute";

    public static string GetEngineName(this ISymbol symbol, FieldType fieldType)
    {
        string? explicitName = TryGetExplicitEngineName(symbol);

        if (!string.IsNullOrEmpty(explicitName))
        {
            return explicitName!;
        }

        return GetEngineName(symbol.Name, fieldType);
    }

    public static string GetEngineName(string sourceName, FieldType fieldType)
    {
        if (string.IsNullOrEmpty(sourceName))
        {
            return sourceName;
        }

        switch (fieldType)
        {
            case FieldType.Class:
                return StripTypePrefix(sourceName, 'U', 'A');

            case FieldType.Interface:
                return StripTypePrefix(sourceName, 'I', 'U');

            case FieldType.Struct:
                return StripTypePrefix(sourceName, 'F');

            case FieldType.Enum:
                return sourceName;

            case FieldType.Delegate:
                return MakeDelegateSignatureName(StripTypePrefix(sourceName, 'F'));

            case FieldType.Unknown:
            default:
                return sourceName;
        }
    }

    public static string MakeDelegateSignatureName(string name)
    {
        return name.EndsWith(DelegateSignatureSuffix) ? name : name + DelegateSignatureSuffix;
    }

    public static FieldType GetFieldType(this ISymbol symbol)
    {
        if (symbol is not ITypeSymbol typeSymbol)
        {
            return FieldType.Unknown;
        }

        switch (typeSymbol.TypeKind)
        {
            case TypeKind.Delegate: return FieldType.Delegate;
            case TypeKind.Enum: return FieldType.Enum;
            case TypeKind.Interface: return FieldType.Interface;
            case TypeKind.Struct: return FieldType.Struct;
            case TypeKind.Class: return FieldType.Class;
            default: return FieldType.Unknown;
        }
    }

    public static bool HasConventionalPrefix(string sourceName, FieldType fieldType)
    {
        if (fieldType == FieldType.Enum || fieldType == FieldType.Unknown)
        {
            return true;
        }

        return !string.Equals(sourceName, GetEngineName(sourceName, fieldType));
    }

    public static string GetConventionalPrefix(FieldType fieldType)
    {
        switch (fieldType)
        {
            case FieldType.Class: return "U or A";
            case FieldType.Interface: return "I";
            case FieldType.Struct: return "F";
            case FieldType.Enum: return "E";
            default: return string.Empty;
        }
    }

    private static string StripTypePrefix(string name, char prefixA, char prefixB = '\0')
    {
        if (name.Length < 3)
        {
            return name;
        }

        char first = name[0];

        if (first != prefixA && (prefixB == '\0' || first != prefixB))
        {
            return name;
        }

        if (!char.IsUpper(name[1]))
        {
            return name;
        }

        return name.Substring(1);
    }

    private static string? TryGetExplicitEngineName(ISymbol symbol)
    {
        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.Name != GeneratedTypeAttributeName)
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is string engineName &&
                !string.IsNullOrEmpty(engineName))
            {
                return engineName;
            }

            return null;
        }

        return null;
    }
}