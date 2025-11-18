using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record BlittableProperty : SimpleProperty
{
    public override string MarshallerType => $"BlittableMarshaller<{ManagedType}>";

    public BlittableProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer) 
        : base(memberSymbol, typeSymbol, propertyType, outer)
    {
        IsBlittable = true;
    }
}