using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record TextProperty : SimpleProperty
{
    public override string MarshallerType => "UnrealSharp.Core.TextMarshaller";

    public TextProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, PropertyType.Text, outer)
    {
    }
}