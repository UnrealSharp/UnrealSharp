using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using UnrealSharp.GlueGenerator.NativeTypes;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator;

public static class PropertyUtilities
{
    public const EPropertyFlags BaseParametersFlags = EPropertyFlags.Parm | EPropertyFlags.BlueprintVisible | EPropertyFlags.BlueprintReadOnly;
    public const EPropertyFlags OutParameterFlags = BaseParametersFlags | EPropertyFlags.OutParm;
    public const EPropertyFlags ReturnParameterFlags = OutParameterFlags | EPropertyFlags.ReturnParm;

    public const EPropertyFlags BaseBlueprintVisiblePropertyFlags = EPropertyFlags.BlueprintVisible;
    public const EPropertyFlags BaseBlueprintReadOnlyPropertyFlags = EPropertyFlags.BlueprintVisible | EPropertyFlags.BlueprintReadOnly;
    public const EPropertyFlags BaseBlueprintReadWritePropertyFlags = EPropertyFlags.BlueprintVisible | EPropertyFlags.BlueprintReadWrite;
    
    public static void MakeParameter(this UnrealProperty property)
    {
        property.PropertyFlags |= BaseParametersFlags;
    }
    
    public static void MakeReturnParameter(this UnrealProperty property)
    {
        property.PropertyFlags |= ReturnParameterFlags;
    }
    
    public static void MakeOutParameter(this UnrealProperty property)
    {
        property.PropertyFlags |= OutParameterFlags;
    }
    
    public static void MakeBlueprintVisibleProperty(this UnrealProperty property)
    {
        property.PropertyFlags |= BaseBlueprintVisiblePropertyFlags;
    }
    
    public static void MakeBlueprintReadOnlyProperty(this UnrealProperty property)
    {
        property.PropertyFlags |= BaseBlueprintReadOnlyPropertyFlags;
    }
    
    public static void MakeBlueprintReadWriteProperty(this UnrealProperty property)
    {
        property.PropertyFlags |= BaseBlueprintReadWritePropertyFlags;
    }
    
    public static void MakeBlueprintAssignable(this UnrealProperty property)
    {
        property.PropertyFlags |= EPropertyFlags.BlueprintAssignable;
    }
    
    public static void PopulateWithArray<T>(this IEnumerable<T>? list, JsonObject baseJsonObject, string arrayName) where T : UnrealType
    {
        if (list == null)
        {
            return;
        }
        
        JsonArray jsonArray = new JsonArray();
        
        foreach (T? item in list)
        {
            JsonObject propertyObject = new JsonObject();
            item.PopulateJsonObject(propertyObject);
            jsonArray.Add(propertyObject);
        }
        
        baseJsonObject[arrayName] = jsonArray;
    }
    
    public static void PopulateWithArray<T>(this IEnumerable<T>? list, JsonObject baseJsonObject, string arrayName, Action<JsonArray> populateAction)
    {
        if (list == null)
        {
            return;
        }
        
        JsonArray jsonArray = new JsonArray();
        populateAction(jsonArray);
        baseJsonObject[arrayName] = jsonArray;
    }
    
    public static void PopulateWithUnrealType(this UnrealType type, JsonObject baseJsonObject, string typeName)
    {
        JsonObject typeObject = new JsonObject();
        type.PopulateJsonObject(typeObject);
        baseJsonObject[typeName] = typeObject;
    }
}