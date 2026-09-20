using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record MapProperty : ContainerProperty
{
    public override bool IsObservable => true;

    public MapProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.Map, outer, syntaxNode)
    {
    }

    protected override string GetFieldMarshaller() => "MapMarshaller";
    protected override string GetObservableMarshaller() => "ObservableMapMarshaller";
    protected override string GetCopyMarshaller() => "MapCopyMarshaller";
}