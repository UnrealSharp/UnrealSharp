using System;
using System.Collections.Generic;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Exporters;
using UnrealSharpManagedGlue.PropertyTranslators;

namespace UnrealSharpManagedGlue.Utilities;

public struct GenericFunctionInfo
{
    public required UhtProperty DeterminesOutputType { get; init; }
    public required UhtProperty DynamicOutputParam { get; init; }
}

public static class FunctionUtilities
{
    public static bool HasAnyFlags(this UhtFunction function, EFunctionFlags flags)
    {
        return (function.FunctionFlags & flags) != 0;
    }
    
    public static bool HasAllFlags(this UhtFunction function, EFunctionFlags flags)
    {
        return (function.FunctionFlags & flags) == flags;
    }

    public static bool IsInterfaceFunction(this UhtFunction function)
    {
        if (function.Outer is not UhtClass classOwner)
        {
            return false;
        }
        
        if (classOwner.HasAnyFlags(EClassFlags.Interface))
        {
            return true;
        }

        string sourceName;
        if (function.SourceName.EndsWith("_Implementation"))
        {
            sourceName = function.SourceName.Substring(0, function.SourceName.Length - 15);
        }
        else
        {
            sourceName = function.SourceName;
        }
        
        UhtClass? currentClass = classOwner;
        while (currentClass != null)
        {
            foreach (UhtClass currentInterface in currentClass.GetInterfaces())
            {
                UhtClass? interfaceClass = currentInterface.GetInterfaceAlternateClass();
                if (interfaceClass != null && interfaceClass.FindFunctionByName(sourceName) != null)
                {
                    return true;
                }
            }
    
            currentClass = currentClass.Super as UhtClass;
        }

        return false;
    }
    
    public static bool HasOutParams(this UhtFunction function)
    {
        // Multicast delegates can have out params, but the UFunction flag isn't set.
        foreach (UhtProperty param in function.Properties)
        {
            if (param.HasAnyFlags(EPropertyFlags.OutParm))
            {
                return true;
            }
        }
        return false;
    }
    
    public static bool HasParametersOrReturnValue(this UhtFunction function)
    {
        return function.HasParameters || function.ReturnProperty != null;
    }
    
    public static string GetNativeFunctionName(this UhtFunction function)
    {
        return $"{function.SourceName}_NativeFunction";
    }

    public static bool HasSameSignature(this UhtFunction function, UhtFunction otherFunction)
    {
        if (function.Children.Count != otherFunction.Children.Count)
        {
            return false;
        }
        
        for (int i = 0; i < function.Children.Count; i++)
        {
            UhtProperty param = (UhtProperty) function.Children[i];
            UhtProperty otherParam = (UhtProperty) otherFunction.Children[i];
            
            if (!param.IsSameType(otherParam))
            {
                return false;
            }
        }
        
        return true;
    }
    
    public static bool IsAutocast(this UhtFunction function)
    {
        if (!function.FunctionFlags.HasAllFlags(EFunctionFlags.Static) || function.ReturnProperty == null || function.Children.Count != 2)
        {            
            return false;
        }

        if (function.Properties.First() is not UhtStructProperty)
        {
            return false;
        }

        // These will be interfaces in C#, which implicit conversion doesn't work for.
        // TODO: Support these in the future.
        UhtProperty returnProperty = function.ReturnProperty!;
        if (returnProperty is UhtArrayProperty or UhtSetProperty or UhtMapProperty)
        {
            return false;
        }

        if (function.HasMetadata("BlueprintAutocast"))
        {
            return true;
        }
        
        string sourceName = function.SourceName;
        return sourceName.StartsWith("Conv_", StringComparison.OrdinalIgnoreCase) || sourceName.StartsWith("To");
    }
    
    public static string GetBlueprintAutocastName(this UhtFunction function)
    {
        int toTypeIndex = function.SourceName.IndexOf("Conv_", StringComparison.Ordinal);
        return toTypeIndex == -1 ? function.SourceName : function.SourceName.Substring(toTypeIndex + 5);
    }
    
    private static bool IsBlueprintAccessor(this UhtFunction function, string accessorType, Func<UhtProperty, UhtFunction?> getBlueprintAccessor)
    {
        if (function.Properties.Count() != 1 )
        {
            return false;
        }
        
        if (function.HasMetadata(accessorType))
        {
            return true;
        }

        if (function.Outer is not UhtClass classObj)
        {
            return false;
        }
        
        foreach (UhtProperty property in classObj.Properties)
        {
            if (function != getBlueprintAccessor(property)! || !function.VerifyBlueprintAccessor(property))
            {
                continue;
            }
                
            return true;
        }

        return false;
    }
    
    public static bool VerifyBlueprintAccessor(this UhtFunction function, UhtProperty property)
    {
        if (!function.Properties.Any() || function.Properties.Count() != 1)
        {
            return false;
        }
            
        UhtProperty firstProperty = function.Properties.First();
        return firstProperty.IsSameType(property);
    }
    
    public static bool IsNativeAccessor(this UhtFunction function, GetterSetterMode accessorType)
    {
        UhtClass classObj = (function.Outer as UhtClass)!;
        foreach (UhtProperty property in classObj.Properties)
        {
            if (accessorType + property.EngineName == function.SourceName)
            {
                switch (accessorType)
                {
                    case GetterSetterMode.Get:
                        return property.HasNativeGetter();
                    case GetterSetterMode.Set:
                        return property.HasNativeSetter();
                }
            }
        }
        
        return false;
    }
    
    public static bool IsAnyGetter(this UhtFunction function)
    {
        if (function.Properties.Count() != 1)
        {
            return false;
        }
        
        return function.IsBlueprintAccessor("BlueprintGetter", property => property.GetBlueprintGetter()) || function.IsNativeAccessor(GetterSetterMode.Get);
    }

    public static bool IsAnySetter(this UhtFunction function)
    {
        if (function.Properties.Count() != 1)
        {
            return false;
        }
        
        return function.IsBlueprintAccessor("BlueprintSetter", property => property.GetBlueprintSetter()) 
               || function.IsNativeAccessor(GetterSetterMode.Set);
    }
    
    public static bool TryGetGenericFunctionInfo(this UhtFunction function, out GenericFunctionInfo info)
    {
        info = default;

        string dotName = function.GetMetadata("DeterminesOutputType");
        string dopName = function.GetMetadata("DynamicOutputParam");
        
        if (!string.IsNullOrEmpty(dotName) && string.IsNullOrEmpty(dopName))
        {
            // If only DOT is specified, DOP is assumed to be the return value.
            dopName = function.HasReturnProperty ? function.ReturnProperty!.EngineName : string.Empty;
        }

        IList<UhtProperty> allParameters = function.GetAllParameters();
        UhtProperty? dot = allParameters.FirstOrDefault(p => p.EngineName == dotName);
        UhtProperty? dop = allParameters.FirstOrDefault(p => p.EngineName == dopName);

        if (dot == null || dop == null)
        {
            return false;
        }

        PropertyTranslator dotTranslator = dot.GetTranslator()!;
        PropertyTranslator dopTranslator = dop.GetTranslator()!;

        if (!dotTranslator.CanSupportGenericType(dot) || !dopTranslator.CanSupportGenericType(dop))
        {
            return false;
        }

        string dotGenericType = dot.GetGenericManagedType();
        string dopGenericType = dop.GetGenericManagedType();
        if (dotGenericType != dopGenericType)
        {
            return false;
        }

        info = new GenericFunctionInfo
        {
            DeterminesOutputType = dot,
            DynamicOutputParam = dop
        };

        return true;
    }

    public static bool HasGenericTypeSupport(this UhtFunction function)
    {
        return function.TryGetGenericFunctionInfo(out _);
    }

    public static string GetGenericTypeConstraint(this UhtFunction function)
    {
        if (!function.HasMetadata("DeterminesOutputType"))
        {
            return string.Empty;
        }

        UhtProperty? propertyDeterminingOutputType = function.Properties.FirstOrDefault(p => p.EngineName == function.GetMetadata("DeterminesOutputType"));

        return propertyDeterminingOutputType?.GetGenericManagedType() ?? string.Empty;
    }

    public static bool HasCustomStructParamSupport(this UhtFunction function)
    {
        if (!function.HasMetadata("CustomStructureParam"))
        {
            return false;
        }

        List<string> customStructParams = function.GetCustomStructParams();
        return customStructParams.All(customParamName => function.Properties.Count(param => param.EngineName == customParamName) == 1);
    }
    
    public static IList<UhtProperty> GetAllParameters(this UhtFunction function)
    {
        IList<UhtProperty> parameters = function.Properties.ToList();

        if (!function.HasReturnProperty)
        {
            return parameters;
        }
        
        parameters.Add(function.ReturnProperty!);
        return parameters;

    }

    public static List<string> GetCustomStructParams(this UhtFunction function)
    {
        if (!function.HasMetadata("CustomStructureParam"))
        {
            return new List<string>();
        }

        return function.GetMetadata("CustomStructureParam").Split(",").ToList();
    }
    
    public static int GetCustomStructParamCount(this UhtFunction function) => function.GetCustomStructParams().Count;
    
    public static List<string> GetCustomStructParamTypes(this UhtFunction function)
    {
        if (!function.HasMetadata("CustomStructureParam"))
        {
            return new List<string>();
        }
        
        int paramCount = function.GetCustomStructParamCount();
        if (paramCount == 1)
        {
            return new List<string> { "CSP" };
        }
        
        return Enumerable.Range(0, paramCount).ToList().ConvertAll(i => $"CSP{i}");
    }

    public static bool IsBlueprintNativeEvent(this UhtFunction function)
    {
        return function.HasAllFlags(EFunctionFlags.BlueprintEvent | EFunctionFlags.Native);
    }

    public static bool IsBlueprintImplementableEvent(this UhtFunction function)
    {
        return function.HasAllFlags(EFunctionFlags.BlueprintEvent) && !function.HasAllFlags(EFunctionFlags.Native);
    }
}