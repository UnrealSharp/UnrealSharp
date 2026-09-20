using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record FieldProperty : SimpleProperty
{
    public FieldName InnerType { get; private set; }

    public FieldProperty(ISymbol symbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer,
        SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, propertyType, outer, syntaxNode)
    {
        InnerType = new FieldName(typeSymbol);
    }

    public FieldProperty(ISymbol symbol, FieldName customFieldName, ITypeSymbol typeSymbol,
        PropertyType propertyType, UnrealType outer, SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, propertyType, outer, syntaxNode)
    {
        InnerType = customFieldName;
    }

    public FieldProperty(PropertyType type, ManagedTypeName managedType, FieldName innerType, string sourceName,
        Accessibility accessibility, UnrealType outer)
        : base(type, managedType, sourceName, accessibility, outer)
    {
        InnerType = innerType;
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