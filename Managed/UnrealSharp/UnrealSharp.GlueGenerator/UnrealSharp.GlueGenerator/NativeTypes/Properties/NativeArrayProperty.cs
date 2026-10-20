using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record NativeArrayProperty : ContainerProperty
{
    public NativeArrayProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer,
        SyntaxNode? syntaxNode = null) : base(symbol, typeSymbol, PropertyType.Array, outer, syntaxNode)
    {
        NeedsMarshallingDelegates = false;
    }

    protected override string GetFieldMarshaller()
    {
        return "NativeArrayMarshaller";
    }

    protected override string GetCopyMarshaller()
    {
        return "NativeArrayCopyMarshaller";
    }
}