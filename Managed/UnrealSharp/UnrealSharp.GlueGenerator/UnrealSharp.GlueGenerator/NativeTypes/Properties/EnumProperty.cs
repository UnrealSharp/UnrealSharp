using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record EnumProperty : FieldProperty
{
    public override string MarshallerType => $"EnumMarshaller<{ManagedType}>";

    public EnumProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, PropertyType.Enum, outer, syntaxNode)
    {
        
    }
}