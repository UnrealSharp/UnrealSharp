using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record NativeArrayProperty : ContainerProperty
{
    public NativeArrayProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) : base(memberSymbol, typeSymbol, PropertyType.Array, outer, syntaxNode)
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