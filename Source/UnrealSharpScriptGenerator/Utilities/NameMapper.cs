using System;
using System.Collections.Generic;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;

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

    public static string GetParameterName(this UhtProperty property)
    {
        string camelCaseName = PascalToCamelCase(property.EngineName);
        return IsReservedKeyword(camelCaseName) ? $"@{camelCaseName}" : camelCaseName;
    }
    
    public static string GetStructName(this UhtType type)
    {
        string scriptName = type.GetMetadata("ScriptName");
        
        if (string.IsNullOrEmpty(scriptName) || scriptName.Contains(' '))
        {
            scriptName = type.EngineName;
        }

        if (type.EngineType is UhtEngineType.Interface or UhtEngineType.NativeInterface)
        {
            scriptName = $"I{scriptName}";
        }
        
        return scriptName;
    }
    
    public static string GetFullManagedName(this UhtType type)
    {
        return $"{type.GetNamespace()}.{type.GetStructName()}";
    }
    
    public static string GetNamespace(this UhtType typeObj)
    {
        UhtType outer = typeObj;

        string packageShortName = "";
        if (outer is UhtPackage package)
        {
            packageShortName = package.ShortName;
        }
        else
        {
            while (outer.Outer != null)
            {
                outer = outer.Outer;
            
                if (outer is UhtHeaderFile header)
                {
                    packageShortName = header.Package.ShortName;
                }
            }
        }
        
        if (string.IsNullOrEmpty(packageShortName))
        {
            throw new Exception($"Failed to find package name for {typeObj}");
        }
        
        return $"UnrealSharp.{packageShortName}";
    }
    
    public static string GetPropertyName(this UhtProperty property)
    {
        string propertyName = property.EngineName;
        if (property.Outer!.EngineName == propertyName || IsReservedKeyword(propertyName))
        {
            propertyName = $"K2_{propertyName}";
        }
        return propertyName;
    }
    
    public static string GetFunctionName(this UhtFunction function)
    {
        string functionName = function.EngineName;

        if (function.HasAnyFlags(EFunctionFlags.Delegate | EFunctionFlags.MulticastDelegate))
        {
            functionName = DelegateBasePropertyTranslator.GetDelegateName(function);
        }
        
        if (functionName.Contains("K2_"))
        {
            functionName = functionName.Replace("K2_", "");
        }

        if (function.Outer is UhtClass classObj)
        {
            foreach (UhtFunction exportedFunction in classObj!.Functions)
            {
                if (exportedFunction != function && functionName == exportedFunction.EngineName)
                {
                    return function.EngineName;
                }
            }  
        }
        
        return functionName;
    }
    
    private static bool IsReservedKeyword(string name)
    {
        return ReservedKeywords.Contains(name);
    }
    
    private static string PascalToCamelCase(string name)
    {
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}