using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record BlittableStructProperty : BlittableProperty
{
    public BlittableStructProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, propertyType, outer, syntaxNode)
    {

    }

    public override void PopulateJsonObject(JsonWriter jsonWriter)
    {
        base.PopulateJsonObject(jsonWriter);
        ManagedType.SerializeToJson(jsonWriter, "InnerType", true);
    }
}