using System.Collections.Generic;
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
    
    public static bool HasMetaData(this UhtFunction function, string key)
    {
        return function.MetaData.ContainsKey(key);
    }
    
    public static string GetMetaData(this UhtFunction function, string key)
    {
        return function.MetaData.GetValueOrDefault(key);
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
            
            List<UhtType> interfaces = new();
            ScriptGeneratorUtilities.GetInterfaces(classOwner, ref interfaces);
            
            foreach (UhtType interfaceType in interfaces)
            {
                if (interfaceType is UhtClass interfaceClass)
                {
                    if (interfaceClass.FindFunctionByName(function.SourceName) != null)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}