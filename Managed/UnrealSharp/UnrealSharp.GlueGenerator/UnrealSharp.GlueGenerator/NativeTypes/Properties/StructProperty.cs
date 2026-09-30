using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record StructProperty : FieldProperty
{
    public override string MarshallerType => ManagedType + "Marshaller";

    public StructProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.Struct, outer, syntaxNode)
    {
    }
}