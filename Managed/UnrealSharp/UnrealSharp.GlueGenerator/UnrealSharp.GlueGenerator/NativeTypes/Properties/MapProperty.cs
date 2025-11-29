using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record MapProperty : ContainerProperty
{
    public MapProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, PropertyType.Map, outer, syntaxNode)
    {

    }
    
    protected override string GetFieldMarshaller() => "MapMarshaller";
    protected override string GetCopyMarshaller() => "MapCopyMarshaller";
}