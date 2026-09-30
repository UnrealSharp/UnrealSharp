using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record BoolProperty : SimpleProperty
{
    public override string MarshallerType => "BoolMarshaller";

    public BoolProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.Bool, outer, syntaxNode)
    {
    }
}