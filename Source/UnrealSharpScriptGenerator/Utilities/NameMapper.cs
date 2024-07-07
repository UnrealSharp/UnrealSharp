using System;
using System.Collections.Generic;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public static class NameMapper
{
    private static readonly List<string> ReservedKeywords = new()
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", 
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", 
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", 
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while", "System"
    };
    
    public static string GetScriptName(this UhtType type)
    {
        if (ReservedKeywords.Contains(type.EngineName))
        {
            return $"K2_{type.EngineName}";
        }
        
        if (type.Outer != null && type.Outer.GetScriptName() == type.EngineName)
        {
            return $"K2_{type.EngineName}";
        }
        
        if (type is UhtClass { ClassType: UhtClassType.Interface } classobj)
        {
            return $"I{classobj.EngineName}";
        }

        // Just return the engine name, no conflicts
        return type.EngineName;
    }
    
    public static string GetScriptName(this UhtFunction function)
    {
        string functionName = GetScriptName((UhtType) function);
        
        if (functionName.Contains("K2_"))
        {
            functionName = functionName.Replace("K2_", "");
        }
        
        UhtClass? classObj = function.Outer as UhtClass;
        if (classObj.EngineName.Contains("TextRenderComponent"))
        {
            Console.WriteLine(functionName);
        }
        foreach (UhtFunction exportedFunction in classObj!.Functions)
        {
            if (exportedFunction != function && functionName == exportedFunction.EngineName)
            {
                return function.SourceName;
            }
        }

        return functionName;
    }
}