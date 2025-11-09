using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record BoolProperty : SimpleProperty
{
    public override string MarshallerType => "BoolMarshaller";
    
    public BoolProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, PropertyType.Bool, outer)
    {
    }
}