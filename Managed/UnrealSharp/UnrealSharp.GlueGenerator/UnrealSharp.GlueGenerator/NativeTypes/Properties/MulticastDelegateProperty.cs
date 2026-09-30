using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record MulticastDelegateProperty : DelegateProperty
{
    private const string MulticastDelegateTypeName = "TMulticastDelegate";
    private const string MulticastDelegateMarshaller = "MulticastDelegateMarshaller";

    public MulticastDelegateProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer,
        SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.MulticastInlineDelegate, outer, MulticastDelegateMarshaller,
            syntaxNode)
    {
    }

    public MulticastDelegateProperty(EquatableArray<UnrealProperty> templateParameters, string sourceName,
        Accessibility accessibility, UnrealType outer)
        : base(templateParameters,
            new ManagedTypeName(MulticastDelegateTypeName),
            PropertyType.MulticastInlineDelegate,
            MulticastDelegateMarshaller,
            sourceName,
            accessibility,
            outer)
    {
    }
}