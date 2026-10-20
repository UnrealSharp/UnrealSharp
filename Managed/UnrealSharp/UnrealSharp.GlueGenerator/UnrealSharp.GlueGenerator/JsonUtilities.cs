using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnrealSharp.GlueGenerator.NativeTypes;

namespace UnrealSharp.GlueGenerator;

public static class JsonKeys
{
    public const string SourceName = "SourceName";
    public const string EngineName = "EngineName";
    public const string Namespace = "Namespace";
    public const string AssemblyName = "AssemblyName";
    public const string FieldType = "FieldType";

    public const string FieldName = "FieldName";
    public const string MetaData = "MetaData";
    public const string Dependencies = "Dependencies";
    public const string Key = "Key";
    public const string Value = "Value";

    public const string Properties = "Properties";
    public const string Functions = "Functions";
    public const string TemplateParameters = "TemplateParameters";
    public const string InnerType = "InnerType";
    public const string ParentClass = "ParentClass";
    public const string Interfaces = "Interfaces";
    public const string Overrides = "Overrides";
    public const string ComponentOverrides = "ComponentOverrides";
    public const string EnumNames = "EnumNames";
}

public static class JsonUtilities
{
    public static void TrySetJsonString(this JsonWriter jsonWriter, string propertyName, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            jsonWriter.WritePropertyName(propertyName);
            jsonWriter.WriteValue(value);
        }
    }

    public static void TrySetJsonNumber(this JsonWriter jsonWriter, string propertyName, int value)
    {
        if (value != 0)
        {
            jsonWriter.WritePropertyName(propertyName);
            jsonWriter.WriteValue(value);
        }
    }

    public static void TrySetJsonEnum<T>(this JsonWriter jsonWriter, string propertyName, T value) where T : Enum
    {
        if (EqualityComparer<T>.Default.Equals(value, default!))
        {
            return;
        }

        ulong raw = Convert.ToUInt64(value);

        jsonWriter.WritePropertyName(propertyName);

        if (raw <= int.MaxValue)
        {
            jsonWriter.WriteValue((long)raw);
        }
        else
        {
            jsonWriter.WriteValue(raw.ToString());
        }
    }

    public static void TrySetJsonBoolean(this JsonWriter jsonWriter, string propertyName, bool value)
    {
        if (value)
        {
            jsonWriter.WritePropertyName(propertyName);
            jsonWriter.WriteValue(value);
        }
    }

    public static void TrySetJsonArray<T>(this JsonWriter jsonWriter, string propertyName, List<T>? values)
    {
        if (values == null || values.Count == 0)
        {
            return;
        }

        jsonWriter.WritePropertyName(propertyName);
        jsonWriter.WriteStartArray();

        foreach (T value in values)
        {
            jsonWriter.WriteValue(value);
        }

        jsonWriter.WriteEndArray();
    }

    public static void TrySetJsonStringArray(this JsonWriter jsonWriter, string propertyName,
        EquatableList<string> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        jsonWriter.WritePropertyName(propertyName);
        jsonWriter.WriteStartArray();

        foreach (string value in values.List)
        {
            jsonWriter.WriteValue(value);
        }

        jsonWriter.WriteEndArray();
    }

    public static void PopulateJsonWithArray<T>(this EquatableList<T> list, JsonWriter jsonWriter, string arrayName)
        where T : UnrealType, IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }

        PopulateJsonWithArray(list.AsEnumerable(), jsonWriter, arrayName);
    }

    public static void PopulateJsonWithArray<T>(this EquatableArray<T> list, JsonWriter jsonWriter, string arrayName)
        where T : UnrealType, IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }

        list.AsEnumerable().PopulateJsonWithArray(jsonWriter, arrayName);
    }

    private static void PopulateJsonWithArray<T>(this IEnumerable<T> list, JsonWriter jsonWriter, string arrayName)
        where T : UnrealType
    {
        jsonWriter.WritePropertyName(arrayName);
        jsonWriter.WriteStartArray();

        foreach (T item in list)
        {
            jsonWriter.WriteStartObject();
            item.PopulateJsonObject(jsonWriter);
            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndArray();
    }

    public static void PopulateJsonWithArray<T>(this EquatableList<T> list, JsonWriter jsonWriter, string arrayName,
        Action<JsonWriter, T> populateAction) where T : IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }

        list.AsEnumerable().PopulateJsonWithArray(jsonWriter, arrayName, populateAction);
    }

    public static void PopulateJsonWithArray<T>(this EquatableArray<T> list, JsonWriter jsonWriter, string arrayName,
        Action<JsonWriter, T> populateAction) where T : IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }

        list.AsEnumerable().PopulateJsonWithArray(jsonWriter, arrayName, populateAction);
    }

    private static void PopulateJsonWithArray<T>(this IEnumerable<T>? list, JsonWriter jsonWriter, string arrayName,
        Action<JsonWriter, T> populateAction)
    {
        if (list == null)
        {
            return;
        }

        jsonWriter.WritePropertyName(arrayName);
        jsonWriter.WriteStartArray();

        foreach (T item in list)
        {
            populateAction(jsonWriter, item);
        }

        jsonWriter.WriteEndArray();
    }

    public static void PopulateJsonWithUnrealType(this UnrealType type, JsonWriter jsonWriter, string typeName)
    {
        jsonWriter.WritePropertyName(typeName);
        jsonWriter.WriteStartObject();
        type.PopulateJsonObject(jsonWriter);
        jsonWriter.WriteEndObject();
    }

    public static string SerializeType(UnrealType type)
    {
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(1024);

        using (System.IO.StringWriter stringWriter = new System.IO.StringWriter(stringBuilder))
        using (JsonTextWriter jsonWriter = new JsonTextWriter(stringWriter))
        {
            jsonWriter.Formatting = Formatting.None;
            jsonWriter.WriteStartObject();
            type.PopulateJsonObject(jsonWriter);
            jsonWriter.WriteEndObject();
        }

        return stringBuilder.ToString();
    }
}