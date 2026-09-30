using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record TextProperty : SimpleProperty
{
    public override string MarshallerType => "UnrealSharp.Core.TextMarshaller";

    public TextProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.Text, outer, syntaxNode)
    {
    }
}