using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record StructProperty : FieldProperty
{
    public override string MarshallerType => ManagedType + "Marshaller";

    public StructProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, PropertyType.Struct, outer, syntaxNode)
    {

    }
}