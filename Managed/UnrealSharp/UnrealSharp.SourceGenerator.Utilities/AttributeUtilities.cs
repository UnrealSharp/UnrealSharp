using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.SourceGenerator.Utilities;

public static class AttributeUtilities
{
    public const string UPropertyAttributeName = "UPropertyAttribute";
    public const string UFunctionAttributeName = "UFunctionAttribute";
    public const string UClassAttributeName = "UClassAttribute";
    public const string UEnumAttributeName = "UEnumAttribute";
    public const string UStructAttributeName = "UStructAttribute";
    public const string CustomLogAttributeName = "CustomLogAttribute";
    
    public const string UMultiDelegateAttributeName = "UMultiDelegateAttribute";
    public const string UDelegateAttributeName = "USingleDelegateAttribute";
    
    public static AttributeData? GetAttribute(this ISymbol symbol, string attributeName)
    {
        ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
        
        foreach (AttributeData? attribute in attributes)
        {
            if (attribute.AttributeClass!.Name == attributeName)
            {
                return attribute;
            }
        }
        
        return null;
    }
    
    public static bool HasAttribute(this ISymbol symbol, string attributeName) => symbol.GetAttribute(attributeName) is not null;
    
    public static AttributeData? TryGetUPropertyAttribute(this ISymbol symbol) => symbol.GetAttribute(UPropertyAttributeName);
    public static AttributeData? TryGetUClassAttribute(this ISymbol symbol) => symbol.GetAttribute(UClassAttributeName);
    public static AttributeData? TryGetUFunctionAttribute(this ISymbol symbol) => symbol.GetAttribute(UFunctionAttributeName);
    public static AttributeData? TryGetUEnumAttribute(this ISymbol symbol) => symbol.GetAttribute(UEnumAttributeName);
    public static AttributeData? TryGetUStructAttribute(this ISymbol symbol) => symbol.GetAttribute(UStructAttributeName);
    public static AttributeData? TryGetUClassAttribute(this INamedTypeSymbol symbol) => symbol.GetAttribute(UClassAttributeName);
    public static AttributeData? TryGetCustomLogAttribute(this ISymbol symbol) => symbol.GetAttribute(CustomLogAttributeName);
    public static AttributeData? TryGetUMultiDelegateAttribute(this ISymbol symbol) => symbol.GetAttribute(UMultiDelegateAttributeName);
    public static AttributeData? TryGetUDelegateAttribute(this ISymbol symbol) => symbol.GetAttribute(UDelegateAttributeName);
    
    public static bool HasUPropertyAttribute(this ISymbol symbol) => HasAttribute(symbol, UPropertyAttributeName);
    public static bool HasUClassAttribute(this ISymbol symbol) => HasAttribute(symbol, UClassAttributeName);
    public static bool HasCustomLogAttribute(this ISymbol symbol) => HasAttribute(symbol, CustomLogAttributeName);
    public static bool HasUFunctionAttribute(this ISymbol symbol) => HasAttribute(symbol, UFunctionAttributeName);
    public static bool HasUEnumAttribute(this ISymbol symbol) => HasAttribute(symbol, UEnumAttributeName);
    public static bool HasUStructAttribute(this ISymbol symbol) => HasAttribute(symbol, UStructAttributeName);
    public static bool HasUMultiDelegateAttribute(this ISymbol symbol) => HasAttribute(symbol, UMultiDelegateAttributeName);
    public static bool HasUDelegateAttribute(this ISymbol symbol) => HasAttribute(symbol, UDelegateAttributeName);
    
    public static bool HasAttribute(this MemberDeclarationSyntax memberDecl, string attributeName)
    {
        foreach (AttributeListSyntax attrList in memberDecl.AttributeLists)
        {
            foreach (AttributeSyntax attribute in attrList.Attributes)
            {
                string name = attribute.Name.ToString();
                string longAttributeName = name.EndsWith("Attribute") ? name : name + "Attribute";
                
                if (longAttributeName == attributeName)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    public static bool HasUPropertyAttribute(this MemberDeclarationSyntax memberDecl) => HasAttribute(memberDecl, UPropertyAttributeName);
    public static bool HasUClassAttribute(this MemberDeclarationSyntax memberDecl) => HasAttribute(memberDecl, UClassAttributeName);
    public static bool HasUFunctionAttribute(this MemberDeclarationSyntax memberDecl) => HasAttribute(memberDecl, UFunctionAttributeName);
    public static bool HasUEnumAttribute(this MemberDeclarationSyntax memberDecl) => HasAttribute(memberDecl, UEnumAttributeName);
    public static bool HasUStructAttribute(this MemberDeclarationSyntax memberDecl) => HasAttribute(memberDecl, UStructAttributeName);
    public static bool HasCustomLogAttribute(this MemberDeclarationSyntax memberDecl) => HasAttribute(memberDecl, CustomLogAttributeName);
    public static bool HasUMultiDelegateAttribute(this MemberDeclarationSyntax memberDecl) => HasAttribute(memberDecl, UMultiDelegateAttributeName);
    public static bool HasUDelegateAttribute(this MemberDeclarationSyntax memberDecl) => HasAttribute(memberDecl, UDelegateAttributeName);
    
    public static bool HasAnyUAttribute(this MemberDeclarationSyntax memberDecl)
    {
        return memberDecl.HasUPropertyAttribute() ||
               memberDecl.HasUFunctionAttribute() ||
               memberDecl.HasUClassAttribute() ||
               memberDecl.HasUEnumAttribute() ||
               memberDecl.HasUStructAttribute() ||
               memberDecl.HasCustomLogAttribute() ||
               memberDecl.HasUMultiDelegateAttribute() ||
               memberDecl.HasUDelegateAttribute();
    }
}