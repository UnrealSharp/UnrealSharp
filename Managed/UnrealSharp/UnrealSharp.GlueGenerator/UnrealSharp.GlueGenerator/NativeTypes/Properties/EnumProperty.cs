using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record EnumProperty : FieldProperty
{
    public override string MarshallerType => $"EnumMarshaller<{ManagedType}>";

    public EnumProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.Enum, outer, syntaxNode)
    {
    }
}