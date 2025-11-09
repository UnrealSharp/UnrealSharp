using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record SimpleProperty : UnrealProperty
{
    public SimpleProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, propertyType, outer)
    {
        ManagedType = new FieldName(typeSymbol);
    }

    public SimpleProperty(PropertyType type, FieldName managedType, string sourceName, Accessibility accessibility, UnrealType outer) : base(type, sourceName, accessibility, outer)
    {
        ManagedType = managedType;
    }
}