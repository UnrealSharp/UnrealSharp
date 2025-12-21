using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record UnrealStruct : UnrealType
{
    public override string EngineName => SourceName.Substring(1);
    
    public EquatableList<UnrealProperty> Properties;
    public bool HasAnyProperties => Properties.Count > 0;
    
    public UnrealStruct(ISymbol typeSymbol, UnrealType? outer = null) : base(typeSymbol, outer)
    {
        Properties = new EquatableList<UnrealProperty>(new List<UnrealProperty>());
    }
    
    public UnrealStruct(string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) 
        : base(sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {

    }

    public override void ExportBackingVariables(GeneratorStringBuilder builder)
    {
        base.ExportBackingVariables(builder);
        
        foreach (UnrealProperty parameter in Properties)
        {
            parameter.ExportBackingVariables(builder);
        }
    }

    public override void ExportBackingVariablesToStaticConstructor(GeneratorStringBuilder builder, string nativeType)
    {
        base.ExportBackingVariablesToStaticConstructor(builder, nativeType);
        Properties.ExportListToStaticConstructor(builder, nativeType);
    }

    public override void PopulateJsonObject(JsonWriter jsonWriter)
    {
        base.PopulateJsonObject(jsonWriter);
        Properties.PopulateJsonWithArray(jsonWriter, "Properties");
    }

    public void AddProperty(UnrealProperty property)
    {
        Properties.List.Add(property);
    }
}