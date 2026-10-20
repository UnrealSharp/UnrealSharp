using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record NameProperty : BlittableProperty
{
    public NameProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.Name, outer, syntaxNode)
    {
    }
}