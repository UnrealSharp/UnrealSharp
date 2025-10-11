using System.Collections.Generic;
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
    
    public void AddProperty(UnrealProperty property)
    {
        Properties.List.Add(property);
    }

    public void AppendProperties(GeneratorStringBuilder builder, List<UnrealProperty>? properties)
    {
        if (properties is null || properties.Count == 0)
        {
            return;
        }
        
        string propertiesArrayPtr = $"{SourceName}Properties";
        builder.AppendLine($"IntPtr {propertiesArrayPtr} = InitStructProps({BuilderNativePtr}, {properties.Count});");
            
        for (int i = 0; i < properties.Count; i++)
        {
            properties[i].MakeProperty(builder, propertiesArrayPtr);
        }
    }
}