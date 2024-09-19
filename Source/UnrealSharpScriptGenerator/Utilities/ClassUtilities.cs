using EpicGames.Core;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public static class ClassUtilities
{
    public static UhtFunction? FindFunctionByName(this UhtClass classObj, string? functionName)
    {
        foreach (UhtFunction function in classObj.Functions)
        {
            if (function.SourceName == functionName)
            {
                return function;
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