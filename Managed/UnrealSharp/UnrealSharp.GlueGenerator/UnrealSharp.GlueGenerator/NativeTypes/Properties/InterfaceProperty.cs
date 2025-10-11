using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record InterfaceProperty : FieldProperty
{
    public override string MarshallerType => ManagedType + "Marshaller";

    public InterfaceProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol? typeSymbol, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, PropertyType.Interface, outer)
    {
        
    }
}