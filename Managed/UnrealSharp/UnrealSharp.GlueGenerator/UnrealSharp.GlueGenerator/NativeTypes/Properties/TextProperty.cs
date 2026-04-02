using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record TextProperty : SimpleProperty
{
    public override string MarshallerType => "UnrealSharp.Core.TextMarshaller";

    public TextProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, PropertyType.Text, outer, syntaxNode)
    {
    }
}