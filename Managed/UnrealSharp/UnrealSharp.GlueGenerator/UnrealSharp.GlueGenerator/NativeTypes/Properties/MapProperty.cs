using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record MapProperty : ContainerProperty
{
    public MapProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, PropertyType.Map, outer)
    {

    }
    
    protected override string GetFieldMarshaller() => "MapMarshaller";
    protected override string GetCopyMarshaller() => "MapCopyMarshaller";
}