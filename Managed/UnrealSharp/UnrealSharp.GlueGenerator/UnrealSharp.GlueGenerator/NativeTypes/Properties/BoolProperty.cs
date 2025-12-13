using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record BoolProperty : SimpleProperty
{
    public override string MarshallerType => "BoolMarshaller";
    
    public BoolProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, PropertyType.Bool, outer, syntaxNode)
    {
    }
}