using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record SoftClassProperty : TemplateProperty
{
    public SoftClassProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer) 
        : base(memberSymbol, typeSymbol, PropertyType.SoftClass, outer, "SoftClassMarshaller")
    {
        
    }
}