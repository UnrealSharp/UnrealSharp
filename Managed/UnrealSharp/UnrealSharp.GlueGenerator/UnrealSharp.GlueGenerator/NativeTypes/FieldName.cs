using System;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public readonly struct FieldName : IEquatable<FieldName>
{
    public readonly string SourceName;
    public readonly string EngineName;
    public readonly string Namespace;
    public readonly string AssemblyName;
    public readonly FieldType FieldType;
    public readonly string FullName;

    public FieldName(ITypeSymbol typeSymbol)
        : this(typeSymbol, typeSymbol.GetFieldType())
    {
    }

    public FieldName(ISymbol symbol, FieldType fieldType)
        : this(symbol.Name, symbol.GetEngineName(fieldType), symbol.GetNamespace(), symbol.ContainingAssembly.Name,
            fieldType)
    {
    }

    public FieldName(string sourceName, string @namespace, string assemblyName, FieldType fieldType)
        : this(sourceName, EngineNaming.GetEngineName(sourceName, fieldType), @namespace, assemblyName, fieldType)
    {
    }

    public FieldName(ISymbol symbol, FieldType fieldType, string sourceName) : this(symbol, fieldType)
    {
        SourceName = sourceName;
    }

    public FieldName(string sourceName, string engineName, string @namespace, string assemblyName, FieldType fieldType)
    {
        SourceName = sourceName;
        EngineName = string.IsNullOrEmpty(engineName) ? SourceName : engineName;
        Namespace = @namespace;
        AssemblyName = assemblyName;
        FieldType = fieldType;
        FullName = MakeFullName(Namespace, SourceName);
    }

    public static FieldName InScopeOf(FieldName scope, string sourceName, FieldType fieldType)
    {
        return new FieldName(sourceName, scope.Namespace, scope.AssemblyName, fieldType);
    }

    public static FieldName Member(string sourceName)
    {
        return new FieldName(sourceName, sourceName, string.Empty, string.Empty, FieldType.Unknown);
    }

    public bool IsValid => !string.IsNullOrEmpty(SourceName);
    public bool IsTypeReference => FieldType != FieldType.Unknown && IsValid;

    public override string ToString() => FullName;

    private static string MakeFullName(string @namespace, string sourceName)
    {
        return string.IsNullOrEmpty(@namespace) ? sourceName : @namespace + "." + sourceName;
    }

    public bool Equals(FieldName other)
    {
        return string.Equals(SourceName, other.SourceName, StringComparison.Ordinal)
               && string.Equals(Namespace, other.Namespace, StringComparison.Ordinal)
               && string.Equals(AssemblyName, other.AssemblyName, StringComparison.Ordinal)
               && FieldType == other.FieldType;
    }

    public override bool Equals(object? obj) => obj is FieldName other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(SourceName);
        hash.Add(Namespace);
        hash.Add(AssemblyName);
        hash.Add((byte)FieldType);
        return hash.ToHashCode();
    }

    public static bool operator ==(FieldName left, FieldName right) => left.Equals(right);
    public static bool operator !=(FieldName left, FieldName right) => !left.Equals(right);

    public void SerializeToJson(JsonWriter writer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(JsonKeys.SourceName);
        writer.WriteValue(SourceName);

        writer.WritePropertyName(JsonKeys.EngineName);
        writer.WriteValue(EngineName);

        writer.TrySetJsonString(JsonKeys.Namespace, Namespace);
        writer.TrySetJsonString(JsonKeys.AssemblyName, AssemblyName);

        writer.WritePropertyName(JsonKeys.FieldType);
        writer.WriteValue((byte)FieldType);

        writer.WriteEndObject();
    }

    public void SerializeToJson(JsonWriter writer, string propertyName)
    {
        if (!IsValid)
        {
            return;
        }

        writer.WritePropertyName(propertyName);
        SerializeToJson(writer);
    }
}