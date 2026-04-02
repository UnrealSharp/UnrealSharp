using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record TaskProperty : TaskPropertyBase
{
    public TaskProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) : base(memberSymbol, typeSymbol, outer, syntaxNode)
    {
    }
}