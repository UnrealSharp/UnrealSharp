using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record NameProperty : BlittableProperty
{
    public NameProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol? typeSymbol, UnrealType outer) : base(syntaxNode, memberSymbol, typeSymbol, PropertyType.Name, outer)
    {
    }
}