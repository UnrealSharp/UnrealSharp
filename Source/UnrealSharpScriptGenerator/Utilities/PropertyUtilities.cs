using System;
using System.Collections.Immutable;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Tooltip;

namespace UnrealSharpScriptGenerator.Utilities;

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