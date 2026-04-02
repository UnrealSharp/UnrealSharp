using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record TaskPropertyBase : TemplateProperty
{
    public TaskPropertyBase(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) : base(memberSymbol, typeSymbol, PropertyType.Unknown, outer, string.Empty, syntaxNode)
    {
    }
}