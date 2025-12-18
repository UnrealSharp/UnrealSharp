using System.Collections.Generic;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    
    public string FullName => Namespace + "." + SourceName;
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
    
    public virtual void PopulateJsonObject(JsonObject jsonObject)
    {
        jsonObject["Name"] = EngineName;
        jsonObject["Namespace"] = Namespace;
        jsonObject["AssemblyName"] = AssemblyName;

        if (SourceGeneratorDependencies.Count > 0)
        {
            JsonArray dependenciesArray = new JsonArray();
            jsonObject["SourceGeneratorDependencies"] = dependenciesArray;
        
            foreach (FieldName dependency in SourceGeneratorDependencies.List)
            {
                dependenciesArray.Add(dependency.SerializeToJson(true));
            }
        }
        
        if (MetaData.Count > 0)
        {
            JsonArray jsonArray = new JsonArray();
            jsonObject["MetaData"] = jsonArray;
            
            foreach (MetaDataInfo metaDataInfo in MetaData.List)
            {
                JsonObject metaDataObject = new JsonObject
                {
                    ["Key"] = metaDataInfo.Key,
                    ["Value"] = metaDataInfo.Value
                };
                jsonArray.Add(metaDataObject);
            }
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