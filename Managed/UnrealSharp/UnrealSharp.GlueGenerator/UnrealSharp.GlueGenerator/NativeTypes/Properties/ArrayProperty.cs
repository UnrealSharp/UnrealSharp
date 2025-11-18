using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record ArrayProperty : ContainerProperty
{
    public ArrayProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer) 
        : base(memberSymbol, typeSymbol, PropertyType.Array, outer)
    {
        
    }
    
    protected override string GetFieldMarshaller() => "ArrayMarshaller";
    protected override string GetCopyMarshaller() => "ArrayCopyMarshaller";
}