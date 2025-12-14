using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record WeakObjectProperty : TemplateProperty
{
    public override string MarshallerType => $"BlittableMarshaller<{ManagedType}>";

    public WeakObjectProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, PropertyType.WeakObject, outer, "BlittableMarshaller", syntaxNode)
    {
    }
}