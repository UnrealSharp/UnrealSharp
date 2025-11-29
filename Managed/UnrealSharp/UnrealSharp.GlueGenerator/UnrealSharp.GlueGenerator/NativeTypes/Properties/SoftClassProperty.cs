using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record SoftClassProperty : TemplateProperty
{
    public SoftClassProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, PropertyType.SoftClass, outer, "SoftClassMarshaller", syntaxNode)
    {
        
    }
}