using System.Collections.Generic;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record UnrealStruct : UnrealType
{
    public override string EngineName => SourceName.Substring(1);
    public EquatableList<UnrealProperty> Properties;
    public bool HasAnyProperties => Properties.Count > 0;
    
    public UnrealStruct(ISymbol typeSymbol, SyntaxNode syntax, UnrealType? outer = null) : base(typeSymbol, syntax, outer)
    {
        Properties = new EquatableList<UnrealProperty>(new List<UnrealProperty>());
    }
    
    public UnrealStruct(string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) 
        : base(sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {

    }

    public override void PopulateJsonObject(JsonObject jsonObject)
    {
        base.PopulateJsonObject(jsonObject);
        Properties.PopulateWithArray(jsonObject, "Properties");
    }

    public void AddProperty(UnrealProperty property)
    {
        Properties.List.Add(property);
    }
}