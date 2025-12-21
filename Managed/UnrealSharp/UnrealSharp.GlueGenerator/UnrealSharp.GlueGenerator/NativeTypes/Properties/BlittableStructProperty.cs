using Microsoft.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record BlittableStructProperty : BlittableProperty
{
    public BlittableStructProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, propertyType, outer, syntaxNode)
    {

    }

    public override void PopulateJsonObject(JObject jsonObject)
    {
        base.PopulateJsonObject(jsonObject);
        ManagedType.SerializeToJson(jsonObject, "InnerType", true);
    }
}