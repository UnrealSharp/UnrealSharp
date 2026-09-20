using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record SingleDelegateProperty : DelegateProperty
{
    public SingleDelegateProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer,
        SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.Delegate, outer, "SingleDelegateMarshaller", syntaxNode)
    {
    }
}