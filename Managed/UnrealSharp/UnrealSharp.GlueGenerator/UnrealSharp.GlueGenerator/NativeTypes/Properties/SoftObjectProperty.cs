using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record SoftObjectProperty : TemplateProperty
{
    public SoftObjectProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer,
        SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.SoftObject, outer, "SoftObjectMarshaller", syntaxNode)
    {
    }
}