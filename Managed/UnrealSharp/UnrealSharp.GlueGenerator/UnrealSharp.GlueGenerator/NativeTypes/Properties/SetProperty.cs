using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record SetProperty : ContainerProperty
{
    public SetProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, PropertyType.Set, outer)
    {
        
    }

    protected override string GetFieldMarshaller() => "SetMarshaller";
    protected override string GetCopyMarshaller() => "SetCopyMarshaller";
}