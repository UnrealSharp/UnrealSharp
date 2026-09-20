using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public readonly record struct MetaDataInfo
{
    public readonly string Key;
    public readonly string Value;

    public MetaDataInfo(string inKey, string inValue)
    {
        Key = inKey ?? string.Empty;
        Value = inValue ?? string.Empty;
    }
}

public record UnrealType
{
    public UnrealType? Outer { get; internal set; }

    public FieldName FieldName { get; internal set; }

    public Accessibility Accessibility { get; set; }

    public EquatableList<FieldName> Dependencies { get; private set; }

    public EquatableList<MetaDataInfo> MetaData { get; private set; }

    public virtual FieldType FieldType => FieldType.Unknown;

    public UnrealType GetOutermost()
    {
        UnrealType current = this;

        while (current.Outer != null)
        {
            current = current.Outer;
        }

        return current;
    }

    public UnrealType(UnrealType? outer = null)
    {
        Outer = outer;
    }

    public UnrealType(ISymbol symbol, UnrealType? outer = null, SyntaxNode? syntaxNode = null) : this(outer)
    {
        FieldName = new FieldName(symbol, symbol.GetFieldType());
        Accessibility = syntaxNode?.GetDeclaredAccessibility() ?? symbol.DeclaredAccessibility;

        List<MetaDataInfo>? metaData = symbol.GetUMetaAttributes();

        if (metaData != null)
        {
            MetaData = new EquatableList<MetaDataInfo>(metaData);
        }
    }

    public UnrealType(string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName,
        UnrealType? outer = null) : this(outer)
    {
        FieldName = new FieldName(sourceName, typeNameSpace, assemblyName, FieldType);
        Accessibility = accessibility;
    }


    public UnrealType(string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName,
        FieldType fieldType, UnrealType? outer) : this(outer)
    {
        FieldName = new FieldName(sourceName, typeNameSpace, assemblyName, fieldType);
        Accessibility = accessibility;
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

    public void AddDependency(UnrealType dependency)
    {
        AddDependency(dependency.FieldName);
    }

    public void AddDependency(FieldName dependency)
    {
        if (!dependency.IsTypeReference)
        {
            return;
        }

        if (dependency == FieldName)
        {
            return;
        }

        if (Dependencies.IsNull)
        {
            Dependencies = new EquatableList<FieldName>(new List<FieldName>());
        }

        if (Dependencies.List.Contains(dependency))
        {
            return;
        }

        Dependencies.List.Add(dependency);
    }

    public virtual void CollectDependencies()
    {
    }

    public virtual void PostParse(ISymbol symbol)
    {
    }

    public virtual void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
    }

    public virtual void ExportBackingVariables(GeneratorStringBuilder builder)
    {
    }

    public virtual void ExportBackingVariablesToStaticConstructor(GeneratorStringBuilder builder, string nativeType)
    {
    }

    public virtual void PopulateJsonObject(JsonWriter jsonWriter)
    {
        FieldName.SerializeToJson(jsonWriter, JsonKeys.FieldName);

        Dependencies.PopulateJsonWithArray(jsonWriter, JsonKeys.Dependencies,
            static (writer, dependency) => { dependency.SerializeToJson(writer); });

        MetaData.PopulateJsonWithArray(jsonWriter, JsonKeys.MetaData, static (writer, metaDataInfo) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName(JsonKeys.Key);
            writer.WriteValue(metaDataInfo.Key);
            writer.WritePropertyName(JsonKeys.Value);
            writer.WriteValue(metaDataInfo.Value);
            writer.WriteEndObject();
        });
    }

    public virtual bool Equals(UnrealType? other)
    {
        if (other is null)
        {
            return false;
        }

        return FieldName == other.FieldName
               && Accessibility == other.Accessibility
               && MetaData.Equals(other.MetaData)
               && Dependencies.Equals(other.Dependencies);
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(FieldName);
        hash.Add((int)Accessibility);
        hash.Add(MetaData);
        hash.Add(Dependencies);
        return hash.ToHashCode();
    }
}