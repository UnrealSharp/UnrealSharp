using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using UnrealSharp.GlueGenerator.NativeTypes;

namespace UnrealSharp.GlueGenerator;

public static class JsonUtilities
{
    public static void TrySetJsonString(this JsonObject jsonObject, string propertyName, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            jsonObject[propertyName] = value;
        }
    }
    
    public static void TrySetJsonNumber(this JsonObject jsonObject, string propertyName, int value)
    {
        if (value != 0)
        {
            jsonObject[propertyName] = value;
        }
    }
    
    public static void TrySetJsonEnum<T>(this JsonObject jsonObject, string propertyName, T value) where T : Enum
    {
        if (!EqualityComparer<T>.Default.Equals(value, default))
        {
            jsonObject[propertyName] = Convert.ToInt64(value);
        }
    }
    
    public static void TrySetJsonBoolean(this JsonObject jsonObject, string propertyName, bool value)
    {
        if (value)
        {
            jsonObject[propertyName] = value;
        }
    }
    
    public static void TrySetJsonArray<T>(this JsonObject jsonObject, string propertyName, List<T>? values)
    {
        if (values != null && values.Count > 0)
        {
            JsonArray jsonArray = new JsonArray();
            foreach (T value in values)
            {
                jsonArray.Add(JsonValue.Create(value));
            }
            jsonObject[propertyName] = jsonArray;
        }
    }
    
    public static void PopulateJsonWithArray<T>(this EquatableList<T> list, JsonObject baseJsonObject, string arrayName) where T : UnrealType, IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }
        
        PopulateJsonWithArray(list.AsEnumerable(), baseJsonObject, arrayName);
    }
    
    public static void PopulateJsonWithArray<T>(this EquatableArray<T> list, JsonObject baseJsonObject, string arrayName) where T : UnrealType, IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }
        
        PopulateJsonWithArray(list.AsEnumerable(), baseJsonObject, arrayName);
    }
    
    static void PopulateJsonWithArray<T>(this IEnumerable<T> list, JsonObject baseJsonObject, string arrayName) where T : UnrealType
    {
        JsonArray jsonArray = new JsonArray();
        
        foreach (T? item in list)
        {
            JsonObject propertyObject = new JsonObject();
            item.PopulateJsonObject(propertyObject);
            jsonArray.Add(propertyObject);
        }
        
        baseJsonObject[arrayName] = jsonArray;
    }
    
    public static void PopulateJsonWithArray<T>(this EquatableList<T> list, JsonObject baseJsonObject, string arrayName, Action<JsonArray> populateAction) where T : IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }
        
        PopulateJsonWithArray(list.AsEnumerable(), baseJsonObject, arrayName, populateAction);
    }
    
    public static void PopulateJsonWithArray<T>(this EquatableArray<T> list, JsonObject baseJsonObject, string arrayName, Action<JsonArray> populateAction) where T : IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }
        
        PopulateJsonWithArray(list.AsEnumerable(), baseJsonObject, arrayName, populateAction);
    }
    
    static void PopulateJsonWithArray<T>(this IEnumerable<T>? list, JsonObject baseJsonObject, string arrayName, Action<JsonArray> populateAction)
    {
        if (list == null)
        {
            return;
        }
        
        JsonArray jsonArray = new JsonArray();
        populateAction(jsonArray);
        baseJsonObject[arrayName] = jsonArray;
    }
    
    public static void PopulateJsonWithUnrealType(this UnrealType type, JsonObject baseJsonObject, string typeName)
    {
        JsonObject typeObject = new JsonObject();
        type.PopulateJsonObject(typeObject);
        baseJsonObject[typeName] = typeObject;
    }
}