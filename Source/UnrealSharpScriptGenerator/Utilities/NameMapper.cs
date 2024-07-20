using System;
using System.Collections.Generic;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;

namespace UnrealSharpScriptGenerator.Utilities;

public enum ENameType
{
    Parameter,
    Property,
    Struct,
    Function
}

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
        string scriptName = ScriptifyName(property.GetScriptName(), ENameType.Parameter);

        if (property.Outer is not UhtFunction function)
        {
            return scriptName;
        }
        
        foreach (UhtProperty exportedProperty in function.Properties)
        {
            if (exportedProperty != property && scriptName == ScriptifyName(exportedProperty.GetScriptName(), ENameType.Parameter))
            {
                return PascalToCamelCase(exportedProperty.EngineName);
            }
        }
        
        return scriptName;
    }
    
    public static string GetPropertyName(this UhtProperty property, List<string> reservedNames)
    {
        string propertyName = ScriptifyName(property.GetScriptName(), ENameType.Property, reservedNames);
        if (property.Outer!.EngineName == propertyName || IsAKeyword(propertyName))
        {
            propertyName = $"K2_{propertyName}";
        }
        return propertyName;
    }
    
    public static string GetStructName(this UhtType type)
    {
        if (type is UhtEnum)
        {
            return type.SourceName;
        }

        string scriptName = type.GetScriptName();

        if (type.EngineType is UhtEngineType.Interface or UhtEngineType.NativeInterface || type == Program.Factory.Session.UInterface)
        {
            scriptName = $"I{scriptName}";
        }
        
        return scriptName;
    }
    
    public static string GetFullManagedName(this UhtType type)
    {
        return $"{type.GetNamespace()}.{type.GetStructName()}";
    }
    
    private static string GetScriptName(this UhtType type)
    {
        string scriptName = type.GetMetadata("ScriptName");
        if (string.IsNullOrEmpty(scriptName) || scriptName.Contains(' '))
        {
            scriptName = type.EngineName;
        }
        return scriptName;
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
    
    public static string GetFunctionName(this UhtFunction function)
    {
        string scriptName = function.GetScriptName();
        string functionName = ScriptifyName(scriptName, ENameType.Function);

        if (function.HasAnyFlags(EFunctionFlags.Delegate | EFunctionFlags.MulticastDelegate))
        {
            functionName = DelegateBasePropertyTranslator.GetDelegateName(function);
        }
        
        if (functionName.Contains("K2_"))
        {
            functionName = functionName.Replace("K2_", "");
        }

        if (function.Outer is not UhtClass classObj)
        {
            return functionName;
        }
        
        foreach (UhtFunction exportedFunction in classObj.Functions)
        {
            if (exportedFunction != function && functionName == exportedFunction.EngineName)
            {
                return function.EngineName;
            }
        } 
            
        if (classObj.EngineName == functionName)
        {
            return "K2_" + functionName;
        }

        return functionName;
    }

    public static string ScriptifyName(string engineName, ENameType nameType)
    {
        string strippedName = engineName;
        switch (nameType)
        {
            case ENameType.Parameter:
                strippedName = StripPropertyPrefix(strippedName);
                strippedName = PascalToCamelCase(strippedName);
                break;
            case ENameType.Property:
                strippedName = StripPropertyPrefix(strippedName);
                break;
            case ENameType.Struct:
                break;
            case ENameType.Function:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(nameType), nameType, null);
        }
        
        return EscapeKeywords(strippedName);
    }

    public static string ScriptifyName(string engineName, ENameType nameType, List<string> reservedNames)
    {
        string strippedName = ScriptifyName(engineName, nameType);
        
        if (nameType is not (ENameType.Parameter or ENameType.Property))
        {
            return strippedName;
        }
        
        if (reservedNames.Contains(strippedName))
        {
            strippedName = engineName;
        }
        
        return strippedName;
    }
    
    public static string StripPropertyPrefix(string inName)
    {
        int nameOffset = 0;

        while (true)
        {
            // Strip the "b" prefix from bool names
            if (inName.Length - nameOffset >= 2 && inName[nameOffset] == 'b' && char.IsUpper(inName[nameOffset + 1]))
            {
                nameOffset += 1;
                continue;
            }

            // Strip the "In" prefix from names
            if (inName.Length - nameOffset >= 3 && inName[nameOffset] == 'I' && inName[nameOffset + 1] == 'n' && char.IsUpper(inName[nameOffset + 2]))
            {
                nameOffset += 2;
                continue;
            }
            break;
        }

        return nameOffset != 0 ? inName.Substring(nameOffset) : inName;
    }
    
    public static string EscapeKeywords(string name)
    {
        return IsAKeyword(name) ? $"@{name}" : name;
    }
    
    private static bool IsAKeyword(string name)
    {
        return ReservedKeywords.Contains(name);
    }
    
    private static string PascalToCamelCase(string name)
    {
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}