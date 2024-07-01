using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;

namespace UnrealSharpScriptGenerator;

public static class ScriptGeneratorUtilities
{
    public const string BlueprintType = "BlueprintType";
    public const string BlueprintSpawnableComponent = "BlueprintSpawnableComponent";
    public const string NotBlueprintType = "NotBlueprintType";
    
    public const string UnrealSharpNamespace = "UnrealSharp";
    public const string EngineNamespace = "UnrealSharp.Engine";
    public const string InteropNamespace = "UnrealSharp.Interop";
    public const string AttributeNamespace = "UnrealSharp.Attributes";

    private static readonly HashSet<string> ForceExportClasses = new()
    {
        "BlueprintFunctionLibrary",
        "DeveloperSettings",
    };
    
    public static bool CanExportClass(UhtClass classObj)
    {
        return classObj.ClassFlags.HasAnyFlags(EClassFlags.RequiredAPI | EClassFlags.MinimalAPI);
    }
    
    public static string GetModuleName(UhtType typeObj)
    {
        if (typeObj.Outer is UhtHeaderFile header)
        {
            return header.Package.ShortName;
        }

        return string.Empty;
    }
    
    public static bool IsConsideredForExporting(UhtType type)
    {
        return !type.MetaData.GetBoolean("NotGeneratorValid");
    }
    
    public static bool CanExportEnum(UhtEnum enumObj)
    {
        return enumObj.MetaData.GetBoolean(BlueprintType);
    }
    
    public static bool CanExportStruct(UhtStruct structObj)
    {
        return structObj.MetaData.GetBoolean(BlueprintType) || HasBlueprintExposedProperties(structObj);
    }

    public static string TryGetPluginDefine(string key)
    {
        Program.PluginModule.TryGetDefine(key, out string? generatedCodePath);
        return generatedCodePath!;
    }
    
    public static bool CanExportFunction(UhtFunction function)
    {
        if (function.FunctionFlags.HasAnyFlags(EFunctionFlags.Delegate))
        {
            return false;
        }

        // Reject if any of the parameter types is unsupported yet
        foreach (UhtType child in function.Children)
        {
            if (child is UhtProperty property && CanExportProperty(property))
            {
                return true;
            }
        }

        return false;
    }
    
    public static bool HasBlueprintExposedProperties(UhtStruct classObj)
    {
        return classObj.Properties.Any(CanExportProperty);
    }
    
    public static bool HasBlueprintExposedFunctions(UhtStruct classObj)
    {
        return classObj.Functions.Any(CanExportFunction);
    }
    
    public static bool CanExportProperty(UhtProperty property)
    {
        return property.PropertyFlags.HasAnyFlags(EPropertyFlags.BlueprintVisible | EPropertyFlags.BlueprintAssignable);
    }
    
    public static bool IsChildOf(this UhtClass? type, string parentClassName)
    {
        UhtClass? currentType = type;
        while (currentType != null)
        {
            if (currentType.EngineClassName == parentClassName)
            {
                return true;
            }
            
            currentType = type!.SuperClass;
        }
        
        return false;
    }
    
    public static string GetCleanTypeName(UhtType type)
    {
        // Remove prefix such as A in AActor
        string typeName = type.SourceName;
        return typeName.Remove(0, 1);
    }
    
    public static void SaveExportedType(UhtType type, BorrowStringBuilder stringBuilder)
    {
        string directory = Path.Combine(Program.GeneratedGluePath, GetModuleName(type));
        string absoluteFilePath = Path.Combine(directory, GetCleanTypeName(type) + ".cs");
        string builtString = stringBuilder.StringBuilder.ToString();
        
        if (File.Exists(absoluteFilePath) && File.ReadAllText(absoluteFilePath) == builtString)
        {
            // No changes, return
            return;
        }
        
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        File.WriteAllText(absoluteFilePath, builtString);
    }
    
    public static bool IsBlueprintExposedType(UhtType classObj)
    {
        UhtType? currentType = classObj;
        while (currentType != null)
        {
            if (currentType.MetaData.GetBoolean(BlueprintType) || currentType.MetaData.GetBoolean(BlueprintSpawnableComponent))
            {
                return true;
            }
            
            if (currentType.MetaData.GetBoolean(NotBlueprintType))
            {
                return false;
            }
            
            if (currentType is UhtClass classType)
            {
                currentType = classType.SuperClass;
            }
            else
            {
                break;
            }
        }

        return false;
    }

    public static void GenerateScriptSkeleton(BorrowStringBuilder stringBuilder)
    {
        
    }
}