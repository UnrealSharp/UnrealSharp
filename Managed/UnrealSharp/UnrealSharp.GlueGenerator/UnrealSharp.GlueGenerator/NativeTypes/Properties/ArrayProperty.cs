using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record ArrayProperty : ContainerProperty
{
    public override bool IsObservable => true;

    public ArrayProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, PropertyType.Array, outer, syntaxNode)
    {
        
    }
    
    protected override string GetFieldMarshaller() => "ArrayMarshaller";
    protected override string GetObservableMarshaller() => "ObservableArrayMarshaller";
    protected override string GetCopyMarshaller() => "ArrayCopyMarshaller";
}