using System;
using System.Collections.Generic;
using EpicGames.Core;
using EpicGames.UHT.Types;

namespace UnrealSharpManagedGlue.Utilities;

public static class ClassUtilities
{
    public static UhtFunction? FindFunctionByName(this UhtClass classObj, string functionName, Func<UhtFunction, string, bool>? customCompare = null, bool includeSuper = false)
        => FindTypeInHierarchy(classObj, c => c.Functions, functionName, customCompare, includeSuper);

    public static UhtProperty? FindPropertyByName(this UhtClass classObj, string propertyName, Func<UhtProperty, string, bool>? customCompare = null, bool includeSuper = false)
        => FindTypeInHierarchy(classObj, c => c.Properties, propertyName, customCompare, includeSuper);

    private static T? FindTypeInHierarchy<T>(UhtClass? classObj, Func<UhtClass, IEnumerable<T>> selector,
        string typeName, Func<T, string, bool>? customCompare, bool includeSuper) where T : UhtType
    {
        for (UhtClass? current = classObj; current != null; current = includeSuper ? current.SuperClass : null)
        {
            T? match = FindTypeByName(typeName, selector(current), customCompare);

            if (match != null)
            {
                return match;
            }

            if (!includeSuper)
            {
                break;
            }
        }

        return null;
    }

    private static T? FindTypeByName<T>(string typeName, IEnumerable<T> types, Func<T, string, bool>? customCompare = null) where T : UhtType
    {
        foreach (var type in types)
        {
            if ((customCompare != null && customCompare(type, typeName)) ||
                string.Equals(type.SourceName, typeName, StringComparison.InvariantCultureIgnoreCase))
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