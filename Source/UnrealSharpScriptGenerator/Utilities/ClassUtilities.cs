using System;
using System.Collections.Generic;
using EpicGames.Core;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public static class ClassUtilities
{
    public static UhtFunction? FindFunctionByName(this UhtClass classObj, string functionName)
    {
        return FindTypeByName(functionName, classObj.Functions);
    }
    
    public static UhtProperty? FindPropertyByName(this UhtClass classObj, string propertyName, Func<UhtProperty, string, bool>? customCompare)
    {
        return FindTypeByName(propertyName, classObj.Properties, customCompare);
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
    
    public static bool HasAnyFlags(this UhtClass classObj, EClassFlags flags)
    {
        return (classObj.ClassFlags & flags) != 0;
    }

    public static bool HasAllFlags(this UhtClass classObj, EClassFlags flags)
    {
        return (classObj.ClassFlags & flags) == flags;
    }
}