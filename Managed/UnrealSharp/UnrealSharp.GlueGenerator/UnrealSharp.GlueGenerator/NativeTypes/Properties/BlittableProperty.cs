using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record BlittableProperty : SimpleProperty
{
    public override string MarshallerType => $"BlittableMarshaller<{ManagedType}>";
    public override bool IsBlittable => true;

    public BlittableProperty(ISymbol symbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer,
        SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, propertyType, outer, syntaxNode)
    {
    }
}