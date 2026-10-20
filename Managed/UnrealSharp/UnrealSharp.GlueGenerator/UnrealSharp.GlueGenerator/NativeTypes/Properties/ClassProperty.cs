using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record ClassProperty : TemplateProperty
{
    public ClassProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.Class, outer, "SubclassOfMarshaller", syntaxNode)
    {
    }
}