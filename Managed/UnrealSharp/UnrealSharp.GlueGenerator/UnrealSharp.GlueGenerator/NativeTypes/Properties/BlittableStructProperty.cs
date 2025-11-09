using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record BlittableStructProperty : BlittableProperty
{
    public BlittableStructProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, propertyType, outer)
    {

    }

    public override void PopulateJsonObject(JsonObject jsonObject)
    {
        base.PopulateJsonObject(jsonObject);
        ManagedType.SerializeToJson(jsonObject, "InnerType", true);
    }
}