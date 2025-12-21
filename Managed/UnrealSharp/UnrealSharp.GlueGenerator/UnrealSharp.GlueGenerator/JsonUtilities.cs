using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnrealSharp.GlueGenerator.NativeTypes;

namespace UnrealSharp.GlueGenerator;

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
        if (!EqualityComparer<T>.Default.Equals(value, default!))
        {
            jsonWriter.WritePropertyName(propertyName);
            jsonWriter.WriteValue(Convert.ToInt64(value));
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
        if (values != null && values.Count > 0)
        {
            jsonWriter.WritePropertyName(propertyName);
            jsonWriter.WriteStartArray();
            foreach (T value in values)
            {
                jsonWriter.WriteValue(value);
            }
            jsonWriter.WriteEndArray();
        }
    }
    
    public static void PopulateJsonWithArray<T>(this EquatableList<T> list, JsonWriter jsonWriter, string arrayName) where T : UnrealType, IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }
        
        PopulateJsonWithArray(list.AsEnumerable(), jsonWriter, arrayName);
    }
    
    public static void PopulateJsonWithArray<T>(this EquatableArray<T> list, JsonWriter jsonWriter, string arrayName) where T : UnrealType, IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }
        
        PopulateJsonWithArray(list.AsEnumerable(), jsonWriter, arrayName);
    }
    
    static void PopulateJsonWithArray<T>(this IEnumerable<T> list, JsonWriter jsonWriter, string arrayName) where T : UnrealType
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
    
    public static void PopulateJsonWithArray<T>(this EquatableList<T> list, JsonWriter jsonWriter, string arrayName, Action<JsonWriter> populateAction) where T : IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }
        
        PopulateJsonWithArray(list.AsEnumerable(), jsonWriter, arrayName, populateAction);
    }
    
    public static void PopulateJsonWithArray<T>(this EquatableArray<T> list, JsonWriter jsonWriter, string arrayName, Action<JsonWriter> populateAction) where T : IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }
        
        PopulateJsonWithArray(list.AsEnumerable(), jsonWriter, arrayName, populateAction);
    }
    
    static void PopulateJsonWithArray<T>(this IEnumerable<T>? list, JsonWriter jsonWriter, string arrayName, Action<JsonWriter> populateAction)
    {
        if (list == null)
        {
            return;
        }
        jsonWriter.WritePropertyName(arrayName);
        jsonWriter.WriteStartArray();
        foreach (T item in list)
        {
            populateAction(jsonWriter);
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
}