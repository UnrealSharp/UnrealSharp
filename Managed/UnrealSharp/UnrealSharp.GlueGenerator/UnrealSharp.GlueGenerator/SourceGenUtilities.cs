using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnrealSharp.GlueGenerator.NativeTypes;

namespace UnrealSharp.GlueGenerator;

public static class SourceGenUtilities
{
    public const string ParamsBuffer = "paramsAlloc";
    public const string ParamsBufferAllocation = "alloc";
    public const string IntPtrZero = "IntPtr.Zero";

    public const string Buffer = "buffer";

    public const string NativeTypePtr = "NativeTypePtr";
    public const string NativeObject = "NativeObject";

    public const string ReturnAssignment = "return ";
    public const string ValueParam = "value";

    public const string ReturnValueName = "ReturnValue";

    public const string ClassKeyword = "class";
    public const string StructKeyword = "struct";
    public const string InterfaceKeyword = "interface";
    public const string EnumKeyword = "enum";
    public const string DelegateKeyword = "delegate";

    public static bool HasAttribute(this ISymbol symbol, string attributeName)
    {
        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (MatchesAttributeName(attribute, attributeName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesAttributeName(AttributeData attribute, string attributeName)
    {
        INamedTypeSymbol? attributeClass = attribute.AttributeClass;

        if (attributeClass is null)
        {
            return false;
        }

        if (attributeName.IndexOf('.') < 0)
        {
            return string.Equals(attributeClass.Name, attributeName, StringComparison.Ordinal);
        }

        return string.Equals(
            attributeClass.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle
                    .Omitted)),
            attributeName,
            StringComparison.Ordinal);
    }

    public static bool HasUFunctionAttribute(this ISymbol symbol)
    {
        return HasAttribute(symbol, "UFunctionAttribute");
    }

    public static string GetFunctionEngineName(this IMethodSymbol methodSymbol)
    {
        foreach (AttributeData attribute in methodSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.Name != "GeneratedFunctionAttribute")
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string name)
            {
                return name;
            }

            break;
        }

        return string.Empty;
    }

    public static List<AttributeData> GetAttributesByName(this ISymbol symbol, string attributeName,
        bool ignoreCase = false)
    {
        ImmutableArray<AttributeData> symbolAttributes = symbol.GetAttributes();
        List<AttributeData> attributes = new List<AttributeData>(symbolAttributes.Length);

        StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        for (int i = 0; i < symbolAttributes.Length; i++)
        {
            AttributeData attribute = symbolAttributes[i];

            if (string.Equals(attribute.AttributeClass?.Name, attributeName, comparison))
            {
                attributes.Add(attribute);
            }
        }

        return attributes;
    }

    public static T? TryGetAttributeConstructorArgument<T>(this AttributeData attribute, int argumentIndex)
    {
        if (attribute.ConstructorArguments.Length <= argumentIndex)
        {
            return default;
        }

        TypedConstant argument = attribute.ConstructorArguments[argumentIndex];

        if (argument.Value is not T value)
        {
            return default;
        }

        return value;
    }

    public static object? TryGetAttributeNamedArgument(this AttributeData attribute, string argumentName)
    {
        foreach (KeyValuePair<string, TypedConstant> namedArgument in attribute.NamedArguments)
        {
            if (namedArgument.Key == argumentName)
            {
                return namedArgument.Value.Value;
            }
        }

        return null;
    }

    public static List<MetaDataInfo>? GetUMetaAttributes(this ISymbol symbol)
    {
        ImmutableArray<AttributeData> symbolAttributes = symbol.GetAttributes();
        List<MetaDataInfo>? attributes = null;

        foreach (AttributeData attribute in symbolAttributes)
        {
            INamedTypeSymbol? attributeClass = attribute.AttributeClass;

            if (attributeClass == null)
            {
                continue;
            }

            if (attributeClass.Name == "UMetaDataAttribute")
            {
                string key = attribute.TryGetAttributeConstructorArgument<string>(0) ?? string.Empty;
                string value = attribute.TryGetAttributeConstructorArgument<string>(1) ?? string.Empty;

                if (key.Length == 0)
                {
                    continue;
                }

                attributes ??= new List<MetaDataInfo>();
                attributes.Add(new MetaDataInfo(key, value));
                continue;
            }

            if (attributeClass.HasAttribute("CustomMetaDataAttribute"))
            {
                string attributeName = attributeClass.Name;

                string key = attributeName.EndsWith("Attribute", StringComparison.Ordinal) &&
                             attributeName.Length > "Attribute".Length
                    ? attributeName.Substring(0, attributeName.Length - "Attribute".Length)
                    : attributeName;

                string value = attribute.TryGetAttributeConstructorArgument<string>(0) ?? string.Empty;

                attributes ??= new List<MetaDataInfo>();
                attributes.Add(new MetaDataInfo(key, value));
            }
        }

        return attributes;
    }

    public static string RefKindToString(this RefKind refKind)
    {
        return refKind switch
        {
            RefKind.None => string.Empty,
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            _ => string.Empty
        };
    }

    public static string AccessibilityToString(this Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Public => "public ",
            Accessibility.Private => "private ",
            Accessibility.Protected => "protected ",
            Accessibility.Internal => "internal ",
            Accessibility.ProtectedOrInternal => "protected internal ",
            Accessibility.ProtectedAndInternal => "private protected ",
            _ => string.Empty
        };
    }

    public static string GetNamespace(this ISymbol symbol)
    {
        if (symbol.ContainingNamespace == null || symbol.ContainingNamespace.IsGlobalNamespace)
        {
            return string.Empty;
        }

        return symbol.ContainingNamespace.ToDisplayString();
    }

    public static string GetEnumNameFromValue(ITypeSymbol enumType, object? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        ulong target;

        try
        {
            target = Convert.ToUInt64(value);
        }
        catch (Exception)
        {
            return value.ToString() ?? string.Empty;
        }

        foreach (ISymbol member in enumType.GetMembers())
        {
            if (member is not IFieldSymbol field || field.ConstantValue is null)
            {
                continue;
            }

            try
            {
                if (Convert.ToUInt64(field.ConstantValue) == target)
                {
                    return field.Name;
                }
            }
            catch (Exception)
            {
            }
        }

        return value.ToString() ?? string.Empty;
    }

    public static void ExportListToStaticConstructor<T>(this EquatableList<T> list, GeneratorStringBuilder builder,
        string nativeType) where T : UnrealType, IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }

        foreach (T item in list)
        {
            item.ExportBackingVariablesToStaticConstructor(builder, nativeType);
        }
    }

    public static Accessibility GetDeclaredAccessibility(this SyntaxNode node)
    {
        SyntaxTokenList modifiers = node switch
        {
            BaseTypeDeclarationSyntax t => t.Modifiers,
            BaseMethodDeclarationSyntax m => m.Modifiers,
            PropertyDeclarationSyntax p => p.Modifiers,
            FieldDeclarationSyntax f => f.Modifiers,
            EventDeclarationSyntax e => e.Modifiers,
            _ => default
        };

        if (modifiers.Count == 0)
        {
            return Accessibility.NotApplicable;
        }

        foreach (SyntaxToken modifier in modifiers)
        {
            switch (modifier.Kind())
            {
                case SyntaxKind.PublicKeyword:
                    return Accessibility.Public;

                case SyntaxKind.PrivateKeyword:
                    return modifiers.Any(SyntaxKind.ProtectedKeyword)
                        ? Accessibility.ProtectedAndInternal
                        : Accessibility.Private;

                case SyntaxKind.ProtectedKeyword:
                    if (modifiers.Any(SyntaxKind.InternalKeyword))
                    {
                        return Accessibility.ProtectedOrInternal;
                    }

                    if (modifiers.Any(SyntaxKind.PrivateKeyword))
                    {
                        return Accessibility.ProtectedAndInternal;
                    }

                    return Accessibility.Protected;

                case SyntaxKind.InternalKeyword:
                    return modifiers.Any(SyntaxKind.ProtectedKeyword)
                        ? Accessibility.ProtectedOrInternal
                        : Accessibility.Internal;
            }
        }

        return Accessibility.NotApplicable;
    }

    public static ISymbol? GetMemberSymbolByName(this INamedTypeSymbol typeSymbol, string memberName)
    {
        ITypeSymbol? currentType = typeSymbol;

        while (currentType != null)
        {
            foreach (ISymbol member in currentType.GetMembers())
            {
                if (member.Name == memberName)
                {
                    return member;
                }
            }

            currentType = currentType.BaseType;
        }

        return null;
    }

    public static bool IsChildOf(this INamedTypeSymbol typeSymbol, INamedTypeSymbol potentialBaseType)
    {
        INamedTypeSymbol? currentBaseType = typeSymbol;

        while (currentBaseType != null)
        {
            if (SymbolEqualityComparer.Default.Equals(currentBaseType, potentialBaseType))
            {
                return true;
            }

            currentBaseType = currentBaseType.BaseType;
        }

        return false;
    }

    public static bool IsChildOf(this INamedTypeSymbol typeSymbol, string potentialBaseTypeName)
    {
        bool qualified = potentialBaseTypeName.IndexOf('.') >= 0;

        SymbolDisplayFormat format = SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

        INamedTypeSymbol? currentBaseType = typeSymbol;

        while (currentBaseType != null)
        {
            string candidate = qualified ? currentBaseType.ToDisplayString(format) : currentBaseType.Name;

            if (string.Equals(candidate, potentialBaseTypeName, StringComparison.Ordinal))
            {
                return true;
            }

            currentBaseType = currentBaseType.BaseType;
        }

        return false;
    }
}