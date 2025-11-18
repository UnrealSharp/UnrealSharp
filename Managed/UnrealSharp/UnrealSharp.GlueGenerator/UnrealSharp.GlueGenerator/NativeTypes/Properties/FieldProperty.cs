using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record FieldProperty : SimpleProperty
{
    public FieldName InnerType;
    
    public FieldProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer) 
        : base(memberSymbol, typeSymbol, propertyType, outer)
    {
        InnerType = new FieldName(typeSymbol);
    }
    
    public FieldProperty(ISymbol memberSymbol, FieldName customFieldName, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer) 
        : base(memberSymbol, typeSymbol, propertyType, outer)
    {
        InnerType = customFieldName;
    }

    public FieldProperty(PropertyType type, FieldName innerType, FieldName fieldName, string sourceName, Accessibility accessibility, UnrealType outer) 
        : base(type, fieldName, sourceName, accessibility, outer)
    {
        InnerType = innerType;
    }

    public override void PopulateJsonObject(JsonObject jsonObject)
    {
        base.PopulateJsonObject(jsonObject);
        InnerType.SerializeToJson(jsonObject, "InnerType", true);
    }
}