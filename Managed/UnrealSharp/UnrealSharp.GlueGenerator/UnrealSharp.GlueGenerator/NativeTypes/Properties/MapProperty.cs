using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record MapProperty : ContainerProperty
{
    public MapProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer) 
        : base(memberSymbol, typeSymbol, PropertyType.Map, outer)
    {

    }
    
    protected override string GetFieldMarshaller() => "MapMarshaller";
    protected override string GetCopyMarshaller() => "MapCopyMarshaller";
}