using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record InterfaceProperty : FieldProperty
{
    public override string MarshallerType => ManagedType + "Marshaller";

    public InterfaceProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, PropertyType.ScriptInterface, outer, syntaxNode)
    {
        
    }
}