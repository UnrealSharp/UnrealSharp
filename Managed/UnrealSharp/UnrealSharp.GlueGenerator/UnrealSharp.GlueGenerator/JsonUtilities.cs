using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnrealSharp.GlueGenerator.NativeTypes;

namespace UnrealSharp.GlueGenerator;

public static class JsonUtilities
{
    public static void TrySetJsonString(this JObject jObject, string propertyName, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            jObject[propertyName] = value;
        }
    }
    
    public static void TrySetJsonNumber(this JObject jObject, string propertyName, int value)
    {
        if (value != 0)
        {
            jObject[propertyName] = value;
        }
    }
    
    public static void TrySetJsonEnum<T>(this JObject jObject, string propertyName, T value) where T : Enum
    {
        if (!EqualityComparer<T>.Default.Equals(value, default!))
        {
            jObject[propertyName] = Convert.ToInt64(value);
        }
    }
    
    public static void TrySetJsonBoolean(this JObject jObject, string propertyName, bool value)
    {
        if (value)
        {
            jObject[propertyName] = value;
        }
    }
    
    public static void PopulateJsonWithArray<T>(this EquatableList<T> list, JObject baseJObject, string arrayName) where T : UnrealType, IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }
        
        PopulateJsonWithArray(list.AsEnumerable(), baseJObject, arrayName);
    }
    
    public static void PopulateJsonWithArray<T>(this EquatableArray<T> list, JObject baseJObject, string arrayName) where T : UnrealType, IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }
        
        PopulateJsonWithArray(list.AsEnumerable(), baseJObject, arrayName);
    }
    
    static void PopulateJsonWithArray<T>(this IEnumerable<T> list, JObject baseJObject, string arrayName) where T : UnrealType
    {
        JArray jsonArray = new JArray();
        
        foreach (T? item in list)
        {
            JObject propertyObject = new JObject();
            item.PopulateJsonObject(propertyObject);
            jsonArray.Add(propertyObject);
        }
        
        baseJObject[arrayName] = jsonArray;
    }
    
    public static void PopulateJsonWithArray<T>(this EquatableList<T> list, JObject baseJObject, string arrayName, Action<JArray> populateAction) where T : IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }
        
        PopulateJsonWithArray(list.AsEnumerable(), baseJObject, arrayName, populateAction);
    }
    
    public static void PopulateJsonWithArray<T>(this EquatableArray<T> list, JObject baseJObject, string arrayName, Action<JArray> populateAction) where T : IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }
        
        PopulateJsonWithArray(list.AsEnumerable(), baseJObject, arrayName, populateAction);
    }
    
    static void PopulateJsonWithArray<T>(this IEnumerable<T>? list, JObject baseJObject, string arrayName, Action<JArray> populateAction)
    {
        if (list == null)
        {
            return;
        }
        
        JArray jsonArray = new JArray();
        populateAction(jsonArray);
        baseJObject[arrayName] = jsonArray;
    }
    
    public static void PopulateJsonWithUnrealType(this UnrealType type, JObject baseJObject, string typeName)
    {
        JObject typeObject = new JObject();
        type.PopulateJsonObject(typeObject);
        baseJObject[typeName] = typeObject;
    }
}