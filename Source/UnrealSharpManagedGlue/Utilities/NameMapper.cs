using System;
using System.Collections.Generic;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.PropertyTranslators;

namespace UnrealSharpManagedGlue.Utilities;

public enum ENameType
{
    Parameter,
    Property,
    Struct,
    Function
}

public static class NameMapper
{
    private static readonly HashSet<string> ReservedKeywords = new()
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
            if (exportedProperty.HasAllFlags(EPropertyFlags.ReturnParm))
            {
                continue;
            }
            
            if (exportedProperty != property && scriptName == ScriptifyName(exportedProperty.GetScriptName(), ENameType.Parameter))
            {
                return PascalToCamelCase(exportedProperty.SourceName);
            }
        }
        
        return scriptName;
    }
    
    public static string GetPropertyName(this UhtProperty property)
    {
        string propertyName = ScriptifyName(property.GetScriptName(), ENameType.Property);
        if (property.Outer!.SourceName == propertyName || IsAKeyword(propertyName))
        {
            propertyName = $"K2_{propertyName}";
        }
        return TryResolveConflictingName(property, propertyName);
    }
    
    public static string GetStructName(this UhtType type)
    {
        if (type.EngineType is UhtEngineType.Interface or UhtEngineType.NativeInterface || type == GeneratorStatics.Factory.Session.UInterface)
        {
            return "I" + type.EngineName;
        }
        
        if (type is UhtClass uhtClass && uhtClass.IsChildOf(GeneratorStatics.BlueprintFunctionLibrary))
        {
            return type.GetScriptName();
        }

        return type.SourceName;
    }

    public static string ExportGetAssemblyName(this UhtType type)
    {
        string structName = type.GetStructName();
        return ExportGetAssemblyName(structName);
    }
    
    public static string ExportGetAssemblyName(string structName)
    {
        return $"typeof({structName}).GetAssemblyName()";
    }
    
    public static string GetFullManagedName(this UhtType type)
    {
        return $"{type.GetNamespace()}.{type.GetStructName()}";
    }
    
    static readonly string[] MetadataKeys = { "ScriptName", "ScriptMethod", "DisplayName" };
    
    public static string GetScriptName(this UhtType type)
    {
        bool OnlyContainsLetters(string str)
        {
            foreach (char c in str)
            {
                if (!char.IsLetter(c) && !char.IsWhiteSpace(c))
                {
                    return false;
                }
            }
            return true;
        }
        
        foreach (var key in MetadataKeys)
        {
            string value = type.GetMetadata(key);
            
            if (string.IsNullOrEmpty(value) || !OnlyContainsLetters(value))
            {
                continue;
            }
            
            // Try remove whitespace from the value
            value = value.Replace(" ", "");
            return value;
        }
        
        return type.SourceName;
    }
    
    public static string GetNamespace(this UhtType typeObj)
    {
        UhtType outer = typeObj;

        string packageShortName = string.Empty;
        if (outer is UhtPackage package)
        {
            packageShortName = package.GetShortName();
        }
        else
        {
            while (outer.Outer != null)
            {
                outer = outer.Outer;
            
                if (outer is UhtPackage header)
                {
                    packageShortName = header.Package.GetShortName();
                    break;
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
        string functionName = function.GetScriptName();

        if (function.HasAnyFlags(EFunctionFlags.Delegate | EFunctionFlags.MulticastDelegate))
        {
            functionName = DelegateBasePropertyTranslator.GetDelegateName(function);
        }
        
        if (functionName.StartsWith("K2_") || functionName.StartsWith("BP_"))
        {
            functionName = functionName.Substring(3);
        }

        if (function.IsInterfaceFunction() && functionName.EndsWith("_Implementation"))
        {
            functionName = functionName.Substring(0, functionName.Length - 15);
        }

        if (function.Outer is not UhtClass)
        {
            return functionName;
        }
        
        functionName = TryResolveConflictingName(function, functionName);

        return functionName;
    }
    
    public static string TryResolveConflictingName(UhtType type, string scriptName)
    {
        UhtType outer = type.Outer!;

        bool IsConflictingWithChild(List<UhtType> children)
        {
            foreach (UhtType child in children)
            {
                if (child == type)
                {
                    continue;
                }
            
                if (child is UhtProperty property)
                {
                    if (scriptName == ScriptifyName(property.GetScriptName(), ENameType.Property))
                    {
                        return true;
                    }
                }
            
                if (child is UhtFunction function)
                {
                    if (scriptName == ScriptifyName(function.GetScriptName(), ENameType.Function))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        bool isConflicting = IsConflictingWithChild(outer.Children);
        
        if (!isConflicting && outer is UhtClass outerClass)
        {
            List<UhtClass> classInterfaces = outerClass.GetInterfaces();
            foreach (UhtClass classInterface in classInterfaces)
            {
                if (classInterface.AlternateObject is not UhtClass interfaceClass)
                {
                    continue;
                }
                
                UhtFunction? function = interfaceClass.FindFunctionByName(scriptName, (uhtFunction, s) => uhtFunction.GetFunctionName() == s);

                if (function == null)
                {
                    continue;
                }
                
                isConflicting = true;
                    
                if (type is UhtFunction typeAsFunction && !function.HasSameSignature(typeAsFunction))
                {
                    isConflicting = false;
                }
                    
                break;
            }
        }
        
        return isConflicting ? type.EngineName : scriptName;
    }

    public static string PrefixWithOuterName(this UhtType type, string name)
    {
        if (type.Outer == null)
        {
            return name;
        }
        
        return $"{type.Outer.SourceName}_{name}";
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
        return IsAKeyword(name) || char.IsDigit(name[0]) ? $"_{name}" : name;
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