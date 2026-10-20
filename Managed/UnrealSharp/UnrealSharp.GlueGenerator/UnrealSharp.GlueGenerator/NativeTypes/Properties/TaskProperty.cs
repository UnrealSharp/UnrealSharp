using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record TaskProperty : TaskPropertyBase
{
    public TaskProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) :
        base(symbol, typeSymbol, outer, syntaxNode)
    {
    }
}