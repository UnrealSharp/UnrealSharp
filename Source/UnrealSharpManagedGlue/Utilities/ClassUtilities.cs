using System;
using System.Collections.Generic;
using EpicGames.Core;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public static class ClassUtilities
{
    public static UhtFunction? FindFunctionByName(this UhtClass classObj, string functionName, Func<UhtFunction, string, bool>? customCompare = null, bool includeSuper = false)
    {
        while (classObj != null)
        {
            UhtFunction? function = FindTypeByName(functionName, classObj.Functions, customCompare);
            
            if (function != null)
            {
                return function;
            }

            if (!includeSuper)
            {
                break;
            }

            classObj = classObj.SuperClass;
        }
        
        return null;
    }
    
    public static UhtProperty? FindPropertyByName(this UhtClass classObj, string propertyName, Func<UhtProperty, string, bool>? customCompare = null, bool includeSuper = false)
    {
        while (classObj != null)
        {
            UhtProperty? property = FindTypeByName(propertyName, classObj.Properties, customCompare);
            
            if (property != null)
            {
                return property;
            }

            if (!includeSuper)
            {
                break;
            }

            classObj = classObj.SuperClass;
        }
        
        return null;
    }
    
    private static T? FindTypeByName<T>(string typeName, IEnumerable<T> types, Func<T, string, bool>? customCompare = null) where T : UhtType
    {
        foreach (var type in types)
        {
            if (customCompare != null && customCompare(type, typeName))
            {
                return type;
            }
            
            if (type.SourceName == typeName
                || (type.SourceName.Length == typeName.Length 
                    && type.SourceName.Contains(typeName, StringComparison.InvariantCultureIgnoreCase)))
            {
                return type;
            }
        }

        return null;
    }

    public static UhtClass? GetInterfaceAlternateClass(this UhtClass thisInterface)
    {
        if (thisInterface.EngineType is not (UhtEngineType.Interface or UhtEngineType.NativeInterface))
        {
            return null;
        }
            
        return thisInterface.AlternateObject as UhtClass;
    }
    
    public static bool HasAnyFlags(this UhtClass classObj, EClassFlags flags)
    {
        return (classObj.ClassFlags & flags) != 0;
    }

    public static bool HasAllFlags(this UhtClass classObj, EClassFlags flags)
    {
        return (classObj.ClassFlags & flags) == flags;
    }
}