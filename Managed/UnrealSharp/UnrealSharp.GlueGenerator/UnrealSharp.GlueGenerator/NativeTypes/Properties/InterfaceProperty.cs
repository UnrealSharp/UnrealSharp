using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record InterfaceProperty : FieldProperty
{
    public override string MarshallerType => ManagedType + "Marshaller";

    public InterfaceProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer,
        SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.ScriptInterface, outer, syntaxNode)
    {
    }
}