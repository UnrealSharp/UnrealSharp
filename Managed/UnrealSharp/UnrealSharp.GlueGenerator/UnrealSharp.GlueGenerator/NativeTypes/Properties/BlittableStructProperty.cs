using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record BlittableStructProperty : BlittableProperty
{
    public BlittableStructProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer) 
        : base(memberSymbol, typeSymbol, propertyType, outer)
    {

    }

    public override void PopulateJsonObject(JsonObject jsonObject)
    {
        base.PopulateJsonObject(jsonObject);
        ManagedType.SerializeToJson(jsonObject, "InnerType", true);
    }
}