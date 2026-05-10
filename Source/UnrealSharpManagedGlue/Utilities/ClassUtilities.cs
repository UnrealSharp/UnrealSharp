using System;
using System.Collections.Generic;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Exporters;

namespace UnrealSharpManagedGlue.Utilities;

public static class ClassUtilities
{
    public static UhtFunction? FindFunctionByName(this UhtClass classObj, string functionName, Func<UhtFunction, string, bool>? customCompare = null, bool includeSuper = false)
        => FindTypeInHierarchy(classObj, c => c.Functions, functionName, customCompare, includeSuper);

    public static UhtProperty? FindPropertyByName(this UhtClass classObj, string propertyName, Func<UhtProperty, string, bool>? customCompare = null, bool includeSuper = false)
        => FindTypeInHierarchy(classObj, c => c.Properties, propertyName, customCompare, includeSuper);

    private static T? FindTypeInHierarchy<T>(UhtClass? classObj, Func<UhtClass, IEnumerable<T>> selector, string typeName, Func<T, string, bool>? customCompare, bool includeSuper) where T : UhtType
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
            if ((customCompare != null && customCompare(type, typeName)) || string.Equals(type.SourceName, typeName, StringComparison.InvariantCultureIgnoreCase))
            {
                return type;
            }
        }

        return null;
    }

    public static UhtClass GetInterfaceAlternateClass(this UhtClass thisInterface)
    {
        return (UhtClass)thisInterface.AlternateObject!;
    }

    public static bool HasAnyFlags(this UhtClass classObj, EClassFlags flags)
    {
        return (classObj.ClassFlags & flags) != 0;
    }

    public static bool HasAllFlags(this UhtClass classObj, EClassFlags flags)
    {
        return (classObj.ClassFlags & flags) == flags;
    }
    
    public static void GetExportedProperties(this UhtStruct structObj, List<UhtProperty> properties, Dictionary<UhtProperty, GetterSetterPair> getterSetterBackedProperties)
    {
        foreach (UhtProperty property in structObj.Properties)
        {
            if (!property.CanExportProperty())
            {
                continue;
            }

            if (structObj.EngineType == UhtEngineType.Class && !getterSetterBackedProperties.ContainsKey(property) && (property.HasAnyGetter() || property.HasAnySetter()))
            {
                GetterSetterPair pair = new GetterSetterPair(property);
                getterSetterBackedProperties.Add(property, pair);
            }
            else
            {
                properties.Add(property);
            }
        }
    }

    public static void GetExportedFunctions(this UhtClass classObj, List<UhtFunction> functions, List<UhtFunction> exportedOverrides, Dictionary<string, GetterSetterPair> getterSetterPairs, Dictionary<string, GetterSetterPair> getSetOverrides)
    {
        List<UhtFunction> exportedFunctions = new();

        bool HasFunction(List<UhtFunction> functionsToCheck, UhtFunction functionToTest)
        {
            foreach (UhtFunction function in functionsToCheck)
            {
                if (function.SourceName == functionToTest.SourceName || function.CppImplName == functionToTest.CppImplName)
                {
                    return true;
                }
            }

            return false;
        }

        foreach (UhtFunction function in classObj.Functions)
        {
            if (!function.CanExportFunction() || function.IsAnyPropertyGetter() || function.IsAnyPropertySetter())
            {
                continue;
            }

            if (function.IsBlueprintEvent())
            {
                exportedOverrides.Add(function);
            }
            else if (function.IsAutocast())
            {
                functions.Add(function);

                if (function.Properties.First() is not UhtStructProperty structToConvertProperty)
                {
                    continue;
                }

                if (structToConvertProperty.Package != function.Package)
                {
                    // For auto-casts to work, they both need to be in the same generated assembly. 
                    // Currently not supported, as we separate engine and project generated assemblies.
                    continue;
                }

                AutocastExporter.AddAutocastFunction(structToConvertProperty.ScriptStruct, function);
            }
            else if (!function.MakeGetterSetterPair(getterSetterPairs))
            {
                functions.Add(function);
            }

            exportedFunctions.Add(function);
        }

        foreach (UhtClass declaration in classObj.GetInterfaces())
        {
            UhtClass interfaceClass = declaration.GetInterfaceAlternateClass();
            foreach (UhtFunction function in interfaceClass.Functions)
            {
                if (function.MakeGetterSetterPair(getSetOverrides) || HasFunction(exportedFunctions, function) || !function.CanExportFunction())
                {
                    continue;
                }

                if (function.IsBlueprintEvent())
                {
                    exportedOverrides.Add(function);
                }
                else
                {
                    functions.Add(function);
                }
            }
        }
    }

    public static List<UhtClass> GetInterfaces(this UhtClass classObj)
    {
        List<UhtClass> interfaces = new();
        foreach (UhtStruct interfaceClass in classObj.Bases)
        {
            UhtEngineType engineType = interfaceClass.EngineType;
            if (engineType is UhtEngineType.Interface or UhtEngineType.NativeInterface)
            {
                interfaces.Add((UhtClass)interfaceClass);
            }
        }

        return interfaces;
    }
}