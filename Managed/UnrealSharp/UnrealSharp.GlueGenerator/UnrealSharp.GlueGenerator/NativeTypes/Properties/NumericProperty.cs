using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record NumericProperty : BlittableProperty
{
    public NumericProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol? typeSymbol, PropertyType propertyType, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, propertyType, outer)
    {

    }
}