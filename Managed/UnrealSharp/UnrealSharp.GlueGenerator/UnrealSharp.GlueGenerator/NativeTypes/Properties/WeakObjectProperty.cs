using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record WeakObjectProperty : TemplateProperty
{
    public override string MarshallerType => $"BlittableMarshaller<{ManagedType}>";

    public WeakObjectProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, PropertyType.WeakObject, outer, "BlittableMarshaller")
    {
    }
}