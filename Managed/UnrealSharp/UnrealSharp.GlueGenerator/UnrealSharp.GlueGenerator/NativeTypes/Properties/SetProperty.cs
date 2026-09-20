using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record SetProperty : ContainerProperty
{
    public SetProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.Set, outer, syntaxNode)
    {
    }

    protected override string GetFieldMarshaller() => "SetMarshaller";
    protected override string GetCopyMarshaller() => "SetCopyMarshaller";
}