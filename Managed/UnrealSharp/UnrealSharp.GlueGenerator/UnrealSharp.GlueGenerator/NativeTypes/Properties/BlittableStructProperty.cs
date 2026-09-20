using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record BlittableStructProperty : BlittableProperty
{
    public FieldName InnerType { get; }

    public BlittableStructProperty(ISymbol symbol, ITypeSymbol typeSymbol, PropertyType propertyType,
        UnrealType outer, SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, propertyType, outer, syntaxNode)
    {
        InnerType = new FieldName(typeSymbol);
    }

    public override void CollectDependencies()
    {
        base.CollectDependencies();
        Outer?.AddDependency(InnerType);
    }

    public override void PopulateJsonObject(JsonWriter jsonWriter)
    {
        base.PopulateJsonObject(jsonWriter);
        InnerType.SerializeToJson(jsonWriter, JsonKeys.InnerType);
    }
}