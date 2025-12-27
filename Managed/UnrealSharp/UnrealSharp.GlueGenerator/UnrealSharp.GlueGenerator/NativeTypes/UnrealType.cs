using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record struct MetaDataInfo
{
    public string Key;
    public string Value;
    
    public MetaDataInfo(string inKey, string inValue)
    {
        Key = inKey;
        Value = inValue;
    }
}

public record UnrealType
{
    public UnrealType? Outer;

    public string SourceName = string.Empty;
    public virtual string EngineName => SourceName;
    public readonly string AssemblyName = string.Empty;
    
    public string FullName => string.IsNullOrEmpty(Namespace) ? SourceName : Namespace + "." + SourceName;
    public string Namespace = string.Empty;
    
    public virtual FieldType FieldType => FieldType.Unknown;
    
    public Accessibility TypeAccessibility;

    public EquatableList<FieldName> SourceGeneratorDependencies;
    public EquatableList<MetaDataInfo> MetaData;
    
    public UnrealType(UnrealType? outer = null)
    {
        Outer = outer;
        SourceName = string.Empty;
        AssemblyName = string.Empty;
        Namespace = string.Empty;
    }
    
    public UnrealType(ISymbol memberSymbol, UnrealType? outer = null, SyntaxNode? syntaxNode = null)
    {
        Outer = outer;
        
        Namespace = memberSymbol.GetNamespace();
        SourceName = memberSymbol.Name;
        AssemblyName = memberSymbol.ContainingAssembly.Name;
        TypeAccessibility = syntaxNode != null ? syntaxNode.GetDeclaredAccessibility() : memberSymbol.DeclaredAccessibility;
        
        List<MetaDataInfo>? metaData = memberSymbol.GetUMetaAttributes();
        if (metaData != null)
        {
            MetaData = new EquatableList<MetaDataInfo>(metaData);
        }
    }

    public UnrealType(string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) : this(outer)
    {
        SourceName = sourceName;
        Namespace = typeNameSpace;
        TypeAccessibility = accessibility;
        AssemblyName = assemblyName;
    }

    public void AddMetaData(string key, string value)
    {
        if (MetaData.IsNull)
        {
            MetaData = new EquatableList<MetaDataInfo>(new List<MetaDataInfo>());
        }
        
        MetaData.List.Add(new MetaDataInfo(key, value));
    }

    public void AddMetaDataRange(IEnumerable<MetaDataInfo> metaData)
    {
        foreach (MetaDataInfo info in metaData)
        {
            AddMetaData(info.Key, info.Value);
        }
    }
    
    public void AddSourceGeneratorDependency(UnrealType dependency)
    {
        AddSourceGeneratorDependency(new FieldName(dependency));
    }

    public void AddSourceGeneratorDependency(FieldName dependency)
    {
        if (SourceGeneratorDependencies.IsNull)
        {
            SourceGeneratorDependencies = new EquatableList<FieldName>(new List<FieldName>());
        }

        SourceGeneratorDependencies.List.Add(dependency);
    }
    
    public virtual void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc) { }
    
    public virtual void ExportBackingVariables(GeneratorStringBuilder builder) { }
    public virtual void ExportBackingVariablesToStaticConstructor(GeneratorStringBuilder builder, string nativeType) { }
    
    public virtual void PopulateJsonObject(JsonWriter jsonWriter)
    {
        jsonWriter.WritePropertyName("Name");
        jsonWriter.WriteValue(EngineName);
        jsonWriter.WritePropertyName("Namespace");
        jsonWriter.WriteValue(Namespace);
        jsonWriter.WritePropertyName("AssemblyName");
        jsonWriter.WriteValue(AssemblyName);

        if (SourceGeneratorDependencies.Count > 0)
        {
            jsonWriter.WritePropertyName("SourceGeneratorDependencies");
            jsonWriter.WriteStartArray();
            foreach (FieldName dependency in SourceGeneratorDependencies.List)
            {
                dependency.SerializeToJson(jsonWriter, true);
            }
            jsonWriter.WriteEndArray();
        }
        
        if (MetaData.Count > 0)
        {
            jsonWriter.WritePropertyName("MetaData");
            jsonWriter.WriteStartArray();            
            foreach (MetaDataInfo metaDataInfo in MetaData.List)
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("Key");
                jsonWriter.WriteValue(metaDataInfo.Key);
                jsonWriter.WritePropertyName("Value");
                jsonWriter.WriteValue(metaDataInfo.Value);
                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndArray();
        }
    }

    public override string ToString()
    {
        return $"{SourceName} (Outer: {Outer?.SourceName ?? "None"})";
    }

    public virtual bool Equals(UnrealType? other)
    {
        if (other is null)
        {
            return false;
        }
        
        return SourceName == other.SourceName && 
               MetaData.Equals(other.MetaData) && 
               Namespace == other.Namespace && 
               TypeAccessibility == other.TypeAccessibility;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = SourceName.GetHashCode();
            hashCode = (hashCode * 397) ^ Namespace.GetHashCode();
            hashCode = (hashCode * 397) ^ MetaData.GetHashCode();
            return hashCode;
        }
    }
}