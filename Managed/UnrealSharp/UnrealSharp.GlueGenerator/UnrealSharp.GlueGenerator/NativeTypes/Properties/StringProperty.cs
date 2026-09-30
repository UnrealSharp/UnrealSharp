using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record StringProperty : SimpleProperty
{
    public override string MarshallerType => "StringMarshaller";

    public StringProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.String, outer, syntaxNode)
    {
    }

    public StringProperty(string sourceName, Accessibility accessibility, UnrealType outer)
        : base(PropertyType.String, new("string"), sourceName, accessibility, outer)
    {
    }
}