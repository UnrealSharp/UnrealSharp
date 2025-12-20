using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
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
    
    public static bool HasAttribute(this ISymbol symbol, string attributeFullName)
    {
        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass is null)
            {
                continue;
            }
            
            if (attribute.AttributeClass.Name == attributeFullName)
            {
                return true;
            }
        }

        return false;
    }
    
    public static bool HasUFunctionAttribute(this ISymbol symbol)
    {
        return HasAttribute(symbol, "UFunctionAttribute");
    }
    
    public static string TryGetEngineName(this ISymbol symbol)
    {
        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass!.Name != "GeneratedTypeAttribute")
            {
                continue;
            }

            return (string) attribute.ConstructorArguments[0].Value!;
        }
        
        return string.Empty;
    }
    
    public static List<AttributeData> GetAttributesByName(this ISymbol symbol, string attributeFullName)
    {
        ImmutableArray<AttributeData> symbolAttributes = symbol.GetAttributes();
        int attributeCount = symbolAttributes.Length;
        
        List<AttributeData> attributes = new List<AttributeData>(attributeCount);
        
        for (int i = 0; i < attributeCount; i++)
        {
            AttributeData attribute = symbolAttributes[i];
            if (attribute.AttributeClass!.Name == attributeFullName)
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
            if (attribute.AttributeClass == null)
            {
                continue;
            }
            
            INamedTypeSymbol? attributeClass = attribute.AttributeClass;
            
            if (attributeClass.Name == "UMetaDataAttribute")
            {
                string key = string.Empty;
                string value = string.Empty;

                if (attribute.ConstructorArguments.Length > 0)
                {
                    TypedConstant argument = attribute.ConstructorArguments[0];
                    if (argument.Value is string argumentValue)
                    {
                        key = argumentValue;
                    }
                }

                if (attribute.ConstructorArguments.Length > 1)
                {
                    TypedConstant argument = attribute.ConstructorArguments[1];
                    if (argument.Value is string argumentValue)
                    {
                        value = argumentValue;
                    }
                }

                if (attributes == null)
                {
                    attributes = new List<MetaDataInfo>();
                }

                attributes.Add(new MetaDataInfo(key, value));
                continue;
            }
            
            if (attributeClass.HasAttribute("CustomMetaDataAttribute"))
            {
                string attributeName = attributeClass.Name;

                int attributeSuffixIndex = attributeName.IndexOf("Attribute", StringComparison.OrdinalIgnoreCase);

                string key;
                if (attributeSuffixIndex >= 0)
                {
                    key = attributeName.Substring(0, attributeSuffixIndex);
                }
                else
                {
                    key = attributeName;
                }

                string value = string.Empty;
                if (attribute.ConstructorArguments.Length > 0)
                {
                    TypedConstant argument = attribute.ConstructorArguments[0];
                    if (argument.Value is string argumentValue)
                    {
                        value = argumentValue;
                    }
                }

                if (attributes == null)
                {
                    attributes = new List<MetaDataInfo>();
                }

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
            _ => throw new ArgumentOutOfRangeException(nameof(refKind), refKind, null)
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
        return symbol.ContainingNamespace.ToDisplayString();
    }
    
    public static string GetEnumNameFromValue(ITypeSymbol enumType, byte value)
    {
        foreach (ISymbol? member in enumType.GetMembers())
        {
            if (member is not IFieldSymbol field || field.ConstantValue is null || (byte) field.ConstantValue != value)
            {
                continue;
            }

            return field.Name;
        }

        return value.ToString();
    }

    public static void ExportListToStaticConstructor<T>(this EquatableList<T> list, GeneratorStringBuilder builder, string nativeType) where T : UnrealType, IEquatable<T>
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
                    return Accessibility.Private;
                case SyntaxKind.ProtectedKeyword:
                    if (modifiers.Any(SyntaxKind.InternalKeyword))
                    {
                        return Accessibility.ProtectedAndInternal;
                    }
                    return Accessibility.Protected;
                case SyntaxKind.InternalKeyword:
                    if (modifiers.Any(SyntaxKind.ProtectedKeyword))
                    {
                        return Accessibility.ProtectedAndInternal;
                    }
                    return Accessibility.Internal;
            }
        }
        
        return Accessibility.NotApplicable;
    }
    
    public static ISymbol? GetMemberSymbolByName(this INamedTypeSymbol typeSymbol, string memberName)
    {
        ISymbol? foundMember = null;
        
        ITypeSymbol? currentType = typeSymbol;
        
        while (currentType != null)
        {
            foreach (ISymbol member in currentType.GetMembers())
            {
                if (member.Name != memberName)
                {
                    continue;
                }
                
                foundMember = member;
                break;
            }

            if (foundMember != null)
            {
                break;
            }
            
            currentType = currentType.BaseType;
        }

        return foundMember;
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
        INamedTypeSymbol? currentBaseType = typeSymbol;
        
        while (currentBaseType != null)
        {
            if (currentBaseType.Name == potentialBaseTypeName)
            {
                return true;
            }
            
            currentBaseType = currentBaseType.BaseType;
        }

        return false;
    }
}