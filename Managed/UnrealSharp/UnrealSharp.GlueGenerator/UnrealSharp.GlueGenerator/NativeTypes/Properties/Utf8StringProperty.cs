using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record Utf8StringProperty : SimpleProperty
{
    public override string MarshallerType => "Utf8StringMarshaller";

    public Utf8StringProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.Utf8String, outer, syntaxNode)
    {
    }
}
