using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record NameProperty : BlittableProperty
{
    public NameProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer) : base(memberSymbol, typeSymbol, PropertyType.Name, outer)
    {
    }
}