using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record WeakObjectProperty : TemplateProperty
{
    public override string MarshallerType => $"BlittableMarshaller<{ManagedType}>";

    public WeakObjectProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer,
        SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.WeakObject, outer, "BlittableMarshaller", syntaxNode)
    {
    }
}