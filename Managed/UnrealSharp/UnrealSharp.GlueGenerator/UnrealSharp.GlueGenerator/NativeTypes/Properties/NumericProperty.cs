using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record NumericProperty : BlittableProperty
{
    public NumericProperty(ISymbol symbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer,
        SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, propertyType, outer, syntaxNode)
    {
    }
}