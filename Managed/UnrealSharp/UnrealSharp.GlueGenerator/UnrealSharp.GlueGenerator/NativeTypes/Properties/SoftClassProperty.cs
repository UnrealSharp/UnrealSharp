using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record SoftClassProperty : TemplateProperty
{
    public SoftClassProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer,
        SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.SoftClass, outer, "SoftClassMarshaller", syntaxNode)
    {
    }
}