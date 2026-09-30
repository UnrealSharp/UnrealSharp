using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record TaskPropertyBase : TemplateProperty
{
    public TaskPropertyBase(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer,
        SyntaxNode? syntaxNode = null) : base(symbol, typeSymbol, PropertyType.Unknown, outer, string.Empty,
        syntaxNode)
    {
    }
}