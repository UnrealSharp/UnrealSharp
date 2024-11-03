using System;
using System.Collections.Generic;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Exporters;

namespace UnrealSharpScriptGenerator.Utilities;

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
        
        while (classOwner.Super != null)
        {
            classOwner = (UhtClass) classOwner.Super;
            
            List<UhtClass> interfaces = classOwner.GetInterfaces();
            foreach (UhtClass interfaceClass in interfaces)
            {
                if (interfaceClass.FindFunctionByName(function.SourceName) != null)
                {
                    return true;
                }
            }
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
        
        return function.IsBlueprintAccessor("BlueprintGetter", property => property.GetBlueprintGetter()) 
               || function.IsNativeAccessor(GetterSetterMode.Get);
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
}