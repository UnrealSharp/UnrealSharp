using System;
using System.Collections.Generic;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Exporters;
using UnrealSharpManagedGlue.PropertyTranslators;

namespace UnrealSharpManagedGlue.Utilities;

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

    public static bool HasBlueprintGetterSetterPair(this UhtProperty property)
    {
        return property.HasBlueprintGetter() && property.HasBlueprintSetter();
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

    public static bool HasGetterSetterPair(this UhtProperty property)
    {
        return property.HasAnyGetter() && property.HasAnySetter();
    }

    public static UhtFunction? GetBlueprintGetter(this UhtProperty property)
    {
        return property.TryGetBlueprintAccessor(GetterSetterMode.Get);
    }

    public static UhtFunction? GetBlueprintSetter(this UhtProperty property)
    {
        return property.TryGetBlueprintAccessor(GetterSetterMode.Set);
    }
    
    public static bool IsWorldContextParameter(this UhtProperty property)
    {
        if (property.Outer is not UhtFunction function)
        {
            return false;
        }

        if (property is not UhtObjectProperty objectProperty || objectProperty.Class != GeneratorStatics.Factory.Session.UObject)
        {
            return false;
        }

        string sourceName = property.SourceName;
        return function.GetMetadata("WorldContext") == sourceName || sourceName is "WorldContextObject" or "WorldContext" or "ContextObject";
    }
    
    public static bool IsReadWrite(this UhtProperty property)
    {
        return !property.IsReadOnly() && (property.PropertyFlags.HasAnyFlags(EPropertyFlags.BlueprintVisible | EPropertyFlags.BlueprintAssignable) || property.HasAnySetter());
    }
    
    public static bool IsReadOnly(this UhtProperty property)
    {
        return property.HasAllFlags(EPropertyFlags.BlueprintReadOnly);
    }
    
    public static bool IsEditDefaultsOnly(this UhtProperty property)
    {
        return property.HasAllFlags(EPropertyFlags.Edit | EPropertyFlags.DisableEditOnInstance);
    }
    
    public static bool IsEditAnywhere(this UhtProperty property)
    {
        return property.HasAllFlags(EPropertyFlags.Edit);
    }
    
    public static bool IsEditInstanceOnly(this UhtProperty property)
    {
        return property.HasAllFlags(EPropertyFlags.Edit | EPropertyFlags.DisableEditOnTemplate);
    }
    
    public static void ForEachInnerProperty(this UhtContainerBaseProperty property, Action<UhtProperty> action)
    {
        action(property.ValueProperty);
        
        if (property is UhtMapProperty mapProperty)
        {
            action(mapProperty.KeyProperty);
        }
    }
    
    public static void ForEachInnerPropertyTranslator(this UhtContainerBaseProperty property, Action<PropertyTranslator> action)
    {
        List<UhtProperty> innerProperties = property.GetInnerProperties();
        foreach (UhtProperty innerProperty in innerProperties)
        {
            PropertyTranslator translator = innerProperty.GetTranslator()!;
            action(translator);
        }
    }
    
    public static List<UhtProperty> GetInnerProperties(this UhtContainerBaseProperty property)
    {
        List<UhtProperty> innerProperties = new List<UhtProperty>(2);
        ForEachInnerProperty(property, innerProperty => innerProperties.Add(innerProperty));
        return innerProperties;
    }
    
    public static List<PropertyTranslator> GetInnerPropertyTranslators(this UhtContainerBaseProperty property)
    {
        List<PropertyTranslator> innerTranslators = new List<PropertyTranslator>(2);
        
        ForEachInnerProperty(property, innerProperty =>
        {
            PropertyTranslator translator = innerProperty.GetTranslator()!;
            innerTranslators.Add(translator);
        });
        
        return innerTranslators;
    }
    
    public static UhtFunction? TryGetBlueprintAccessor(this UhtProperty property, GetterSetterMode accessorType)
    {
        if (property.Outer is UhtScriptStruct || property.Outer is not UhtClass classObj)
        {
            return null;
        }
        
        UhtFunction? TryFindFunction(string? name)
        {
            if (name is null)
            {
                return null;
            }
            
            UhtFunction? function = classObj.FindFunctionByName(name, (uhtFunction, typeName) =>
            {
                if (uhtFunction.SourceName == typeName
                    || (uhtFunction.SourceName.Length == typeName.Length
                        && uhtFunction.SourceName.Contains(typeName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return true;
                }

                if (uhtFunction.GetScriptName() == typeName
                    || (uhtFunction.GetScriptName().Length == typeName.Length
                        && uhtFunction.GetScriptName().Contains(typeName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return true;
                }

                return false;
            });

            if (function != null && function.VerifyBlueprintAccessor(property))
            {
                return function;
            }

            return null;
        }
        
        string accessorName = GetAccessorName(property, accessorType);
        UhtFunction? function = TryFindFunction(accessorName);
        if (function != null)
        {
            return function;
        }

        function = TryFindFunction(accessorType + property.SourceName);
        if (function != null)
        {
            return function;
        }

        function = TryFindFunction(accessorType + property.GetPropertyName());
        if (function != null)
        {
            return function;
        }

        function = TryFindFunction(accessorType + NameMapper.ScriptifyName(property.SourceName, ENameType.Property));
        if (function != null)
        {
            return function;
        }

        return null;
    }

    private static string GetAccessorName(UhtProperty property, GetterSetterMode accessorType)
    {
        return accessorType == GetterSetterMode.Get
            ? property.Getter ?? property.GetMetaData("BlueprintGetter")
            : property.Setter ?? property.GetMetaData("BlueprintSetter");
    }

    public static string GetNativePropertyName(this UhtProperty property)
    {
        return $"{property.SourceName}_NativeProperty";
    }
    
    public static string GetOffsetVariableName(this UhtProperty property)
    {
        return $"{property.Outer!.SourceName}_{property.SourceName}_Offset";
    }

    public static string GetProtection(this UhtProperty property)
    {
        UhtClass? classObj = property.Outer as UhtClass;
        bool isClassOwner = classObj != null;

        if (isClassOwner)
        {
            UhtFunction? getter = property.GetBlueprintGetter();
            UhtFunction? setter = property.GetBlueprintSetter();

            if ((getter != null && getter.FunctionFlags.HasAnyFlags(EFunctionFlags.Public)) || (setter != null && setter.FunctionFlags.HasAnyFlags(EFunctionFlags.Public)))
            {
                return ScriptGeneratorUtilities.PublicKeyword;
            }

            if ((getter != null && getter.FunctionFlags.HasAnyFlags(EFunctionFlags.Protected)) || (setter != null && setter.FunctionFlags.HasAnyFlags(EFunctionFlags.Protected)))
            {
                return ScriptGeneratorUtilities.ProtectedKeyword;
            }
        }

        if (property.HasAllFlags(EPropertyFlags.NativeAccessSpecifierPublic) ||
            (property.HasAllFlags(EPropertyFlags.NativeAccessSpecifierPrivate) && property.HasMetaData("AllowPrivateAccess")) ||
            (!isClassOwner && property.HasAllFlags(EPropertyFlags.Protected)))
        {
            return ScriptGeneratorUtilities.PublicKeyword;
        }

        if (isClassOwner && property.HasAllFlags(EPropertyFlags.Protected))
        {
            return ScriptGeneratorUtilities.ProtectedKeyword;
        }
        
        if (property.HasAllFlags(EPropertyFlags.Edit))
        {
            return ScriptGeneratorUtilities.PublicKeyword;
        }

        return ScriptGeneratorUtilities.PrivateKeyword;
    }

    public static bool IsGenericType(this UhtProperty property)
    {
        UhtFunction? function = property.GetTypedOuter<UhtFunction>();
        if (function == null)
        {
            return false;
        }
        
        if (!function.TryGetGenericFunctionInfo(out GenericFunctionInfo info))
        {
            return false;
        }

        string propertyGenericType = property.GetGenericManagedType();
        string determinesGenericType = info.DeterminesOutputType.GetGenericManagedType();
        return propertyGenericType == determinesGenericType;
    }

    public static string GetGenericManagedType(this UhtProperty property)
    {
        return property switch
        {
            UhtSoftObjectProperty softClass => softClass.Class.GetFullManagedName(),
            UhtClassProperty softClass => softClass.MetaClass!.GetFullManagedName(),
            UhtObjectProperty objProp => objProp.Class.GetFullManagedName(),
            UhtContainerBaseProperty container => GetGenericManagedType(container.ValueProperty),
            UhtObjectPropertyBase objBase => objBase.MetaClass!.GetFullManagedName(),
            _ => string.Empty
        };
    }
    
    public static bool IsCustomStructureType(this UhtProperty property)
    {
        if (property.Outer is not UhtFunction function)
        {
            return false;
        }
        
        if (!function.HasCustomStructParamSupport())
        {
            return false;
        }

        if (function.GetCustomStructParams().Contains(property.EngineName))
        {
            PropertyTranslator translator = property.GetTranslator()!;
            return translator.CanSupportCustomStruct(property);
        }

        return false;
    }

    public static List<UhtProperty>? GetPrecedingParams(this UhtProperty property)
    {
        if (property.Outer is not UhtFunction function)
        {
            return null;
        }
        
        return function.Children.Cast<UhtProperty>().TakeWhile(param => param != property).ToList();
    }
    
    public static int GetPrecedingCustomStructParams(this UhtProperty property)
    {
        if (property.Outer is not UhtFunction function)
        {
            return 0;
        }

        if (!function.HasCustomStructParamSupport())
        {
            return 0;
        }

        return property.GetPrecedingParams()!.Count(param => param.IsCustomStructureType());
    }
}
