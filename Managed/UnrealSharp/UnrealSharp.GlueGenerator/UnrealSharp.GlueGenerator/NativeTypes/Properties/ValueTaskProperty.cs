using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record ValueTaskProperty : TaskPropertyBase
{
    public ValueTaskProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer,
        SyntaxNode? syntaxNode = null) : base(symbol, typeSymbol, outer, syntaxNode)
    {
    }
}