using System.Collections.Generic;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;

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
}