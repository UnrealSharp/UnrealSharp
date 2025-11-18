using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record NumericProperty : BlittableProperty
{
    public NumericProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer) 
        : base(memberSymbol, typeSymbol, propertyType, outer)
    {

    }
}