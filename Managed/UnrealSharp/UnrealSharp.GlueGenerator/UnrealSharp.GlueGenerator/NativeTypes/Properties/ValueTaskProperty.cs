using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record ValueTaskProperty : TaskPropertyBase
{
    public ValueTaskProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) : base(memberSymbol, typeSymbol, outer, syntaxNode)
    {
    }
}