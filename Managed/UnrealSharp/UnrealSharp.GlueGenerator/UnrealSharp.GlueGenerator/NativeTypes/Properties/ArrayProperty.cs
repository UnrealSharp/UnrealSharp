using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record ArrayProperty : ContainerProperty
{
    public ArrayProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, PropertyType.Array, outer)
    {
        
    }
    
    protected override string GetFieldMarshaller() => "ArrayMarshaller";
    protected override string GetCopyMarshaller() => "ArrayCopyMarshaller";
}