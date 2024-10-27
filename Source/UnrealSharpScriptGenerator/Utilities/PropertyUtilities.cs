using EpicGames.Core;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public enum AccessorType
{
    Getter,
    Setter
}

public static class PropertyUtilities
{
    public static bool IsOuter<T>(this UhtProperty property)
    {
        return property.Outer is T;
    }
    
    public static bool HasAnyFlags(this UhtProperty property, EPropertyFlags flags)
    {
        return (property.PropertyFlags & flags) != 0;
    }
    
    public static bool HasAllFlags(this UhtProperty property, EPropertyFlags flags)
    {
        return (property.PropertyFlags & flags) == flags;
    }
    
    public static string GetMetaData(this UhtProperty property, string key)
    {
        return property.MetaData.TryGetValue(key, out var value) ? value : string.Empty;
    }
    
    public static bool HasMetaData(this UhtProperty property, string key)
    {
        return property.MetaData.ContainsKey(key);
    }
    
    public static bool HasNativeGetter(this UhtProperty property)
    {
        if (property.Outer is UhtScriptStruct)
        {
            return false;
        }
        
        return !string.IsNullOrEmpty(property.Getter);
    }
    
    public static bool HasNativeSetter(this UhtProperty property)
    {
        if (property.Outer is UhtScriptStruct)
        {
            return false;
        }
        
        return !string.IsNullOrEmpty(property.Setter);
    }
    
    public static bool HasAnyNativeGetterSetter(this UhtProperty property)
    {
        return property.HasNativeGetter() || property.HasNativeSetter();
    }
    
    public static bool HasBlueprintGetter(this UhtProperty property)
    {
        return property.GetBlueprintGetter() != null;
    }
    
    public static bool HasBlueprintSetter(this UhtProperty property)
    {
        return property.GetBlueprintSetter() != null;
    }
    
    public static bool HasBlueprintGetterOrSetter(this UhtProperty property)
    {
        return property.HasBlueprintGetter() || property.HasBlueprintSetter();
    }
    
    public static bool HasAnyGetterOrSetter(this UhtProperty property)
    {
        return property.HasAnyNativeGetterSetter() || property.HasBlueprintGetterOrSetter();
    }
    
    public static bool HasAnyGetter(this UhtProperty property)
    {
        return property.HasNativeGetter() || property.HasBlueprintGetter();
    }
    
    public static bool HasAnySetter(this UhtProperty property)
    {
        return property.HasNativeSetter() || property.HasBlueprintSetter();
    }
    
    public static UhtFunction? GetBlueprintGetter(this UhtProperty property)
    {
        return property.TryGetNativeAccessor(AccessorType.Getter);
    }
    
    public static UhtFunction? GetBlueprintSetter(this UhtProperty property)
    {
        return property.TryGetNativeAccessor(AccessorType.Setter);
    }
    
    public static bool HasReadWriteAccess(this UhtProperty property)
    {
        return !property.HasAnyFlags(EPropertyFlags.BlueprintReadOnly) || property.HasAnyGetterOrSetter();
    }
    
    public static UhtFunction? TryGetNativeAccessor(this UhtProperty property, AccessorType accessorType)
    {
        if (property.Outer is UhtScriptStruct)
        {
            return null;
        }
        
        UhtClass classObj = (property.Outer as UhtClass)!;
        string blueprintGetter = property.GetMetaData(accessorType == AccessorType.Getter ? "BlueprintGetter" : "BlueprintSetter");
        UhtFunction? function = classObj.FindFunctionByName(blueprintGetter);
        
        if (function != null && function.VerifyAccessor(property))
        {
            return function;
        }
        
        string accessorName = accessorType == AccessorType.Getter ? "Get" : "Set";
        function = classObj.FindFunctionByName(accessorName + property.EngineName);
        if (function != null && function.VerifyAccessor(property))
        {
            return function;
        }

        return null;
    }
    
    public static string GetNativePropertyName(this UhtProperty property)
    {
        return $"{property.SourceName}_NativeProperty";
    }

    public static string GetProtection(this UhtProperty property)
    {
        string blueprintGetter = property.GetMetaData("BlueprintGetter");
        string blueprintSetter = property.GetMetaData("BlueprintSetter");
        UhtClass? classObj = property.Outer as UhtClass;
        bool isClassOwner = classObj != null;
        
        if (isClassOwner && blueprintGetter != string.Empty || blueprintSetter != string.Empty)
        {
            UhtFunction? getter = classObj!.FindFunctionByName(blueprintGetter);
            UhtFunction? setter = classObj!.FindFunctionByName(blueprintSetter);
            
            if ((getter != null && getter.FunctionFlags.HasAnyFlags(EFunctionFlags.Public) || (setter != null && setter.FunctionFlags.HasAnyFlags(EFunctionFlags.Public))))
            {
                return "public ";
            }
            
            if ((getter != null && getter.FunctionFlags.HasAnyFlags(EFunctionFlags.Protected) || (setter != null && setter.FunctionFlags.HasAnyFlags(EFunctionFlags.Protected))))
            {
                return "protected ";
            }
        }

        if (property.HasAllFlags(EPropertyFlags.NativeAccessSpecifierPublic) ||
            (property.HasAllFlags(EPropertyFlags.NativeAccessSpecifierPrivate) && property.HasMetaData("AllowPrivateAccess")) ||
            (!isClassOwner && property.HasAllFlags(EPropertyFlags.Protected)))
        {
            return "public ";
        }
        else if (isClassOwner && property.HasAllFlags(EPropertyFlags.Protected))
        {
            return "protected ";
        }
        else
        {
            return "private ";
        }
    }
}