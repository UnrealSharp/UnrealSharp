using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record FieldProperty : SimpleProperty
{
    public FieldName InnerType;
    protected virtual bool StripPrefix => true;
    
    public FieldProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, propertyType, outer, syntaxNode)
    {
        InnerType = new FieldName(typeSymbol);
    }
    
    public FieldProperty(ISymbol memberSymbol, FieldName customFieldName, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer, SyntaxNode? syntaxNode = null)
        : base(memberSymbol, typeSymbol, propertyType, outer, syntaxNode)
    {
        InnerType = customFieldName;
    }

    public FieldProperty(PropertyType type, FieldName innerType, FieldName fieldName, string sourceName, Accessibility accessibility, UnrealType outer) 
        : base(type, fieldName, sourceName, accessibility, outer)
    {
        InnerType = innerType;
    }

    public override void PopulateJsonObject(JsonWriter jsonWriter)
    {
        base.PopulateJsonObject(jsonWriter);
        InnerType.SerializeToJson(jsonWriter, "InnerType", StripPrefix);
    }
}