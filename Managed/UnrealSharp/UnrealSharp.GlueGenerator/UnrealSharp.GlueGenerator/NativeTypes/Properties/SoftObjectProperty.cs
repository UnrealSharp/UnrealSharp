using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record SoftObjectProperty : TemplateProperty
{
    public SoftObjectProperty(ISymbol memberSymbol, ITypeSymbol? typeSymbol, UnrealType outer) 
        : base(memberSymbol, typeSymbol, PropertyType.SoftObject, outer, "SoftObjectMarshaller")
    {
        
    }
}