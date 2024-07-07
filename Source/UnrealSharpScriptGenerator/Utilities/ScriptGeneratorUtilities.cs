using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;

namespace UnrealSharpScriptGenerator.Utilities;

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
        if (classObj.EngineName == "Object")
        {
            return true;
        }
        
        return classObj.ClassFlags.HasAnyFlags(EClassFlags.RequiredAPI | EClassFlags.MinimalAPI);
    }
    
    public static string GetNamespace(UhtType typeObj)
    {
        UhtType outer = typeObj;
        while (outer.Outer != null)
        {
            outer = outer.Outer;
            
            if (outer is UhtHeaderFile header)
            {
                return "UnrealSharp." + header.Package.ShortName;
            }
        }

        return string.Empty;
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
        // if (function.HasMetadata("BlueprintInternalUseOnly"))
        // {
        //     if (!(function.Outer is UhtClass uhtClass && uhtClass.IsChildOf("BlueprintAsyncActionBase")))
        //     {
        //         return false;
        //     }
        // }
        
        foreach (UhtProperty child in function.Properties)
        {
            if (!CanExportProperty(child))
            {
                return false;
            }
        }

        return true;
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
        if (property.MetaData.GetBoolean("ScriptNoExport"))
        {
            return false;
        }
        
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property);
        
        if (translator == null)
        {
            return false;
        }

        bool isClassProperty = property.Outer is UhtClass;

        return (isClassProperty && !translator.IsSupportedAsProperty()) || (!isClassProperty && !translator.IsSupportedAsStructProperty()) || !translator.CanExport(property);
    }
    
    public static string GetCleanEnumValueName(UhtEnum enumObj, UhtEnumValue enumValue)
    {
        if (enumObj.CppForm == UhtEnumCppForm.Regular)
        {
            return enumValue.Name;
        }
        
        int delimiterIndex = enumValue.Name.IndexOf("::", StringComparison.Ordinal);
        return delimiterIndex < 0 ? enumValue.Name : enumValue.Name.Substring(delimiterIndex + 2);
    }
    
    public static bool IsChildOf(this UhtClass? type, string parentClassName)
    {
        UhtClass? currentType = type;
        while (currentType != null)
        {
            if (currentType.EngineName == parentClassName)
            {
                return true;
            }
            
            currentType = currentType.SuperClass;
        }
        
        return false;
    }
    
    public static string GetFullManagedName(UhtType type)
    {
        return $"{GetNamespace(type)}.{type.EngineName}";
    }
    
    public static void GetExportedProperties(UhtStruct structObj, ref List<UhtProperty> properties)
    {
        if (!structObj.Properties.Any())
        {
            return;
        }
        
        foreach (UhtProperty property in structObj.Properties)
        {
            if (CanExportProperty(property))
            {
                properties.Add(property);
            }
        }
    }
    
    public static void GetExportedFunctions(UhtClass classObj, ref List<UhtFunction> functions, ref List<UhtFunction> overridableFunctions)
    {
        if (!classObj.Functions.Any())
        {
            return;
        }

        foreach (UhtFunction function in classObj.Functions)
        {
            if (!CanExportFunction(function))
            {
                continue;
            }
            
            if (function.FunctionFlags.HasAnyFlags(EFunctionFlags.BlueprintEvent))
            {
                overridableFunctions.Add(function);
            }
            else
            {
                functions.Add(function);
            }
        }
        
        if (classObj.Declarations == null)
        {
            return;
        }
        
        foreach (UhtStruct declaration in classObj.Bases)
        {
            if (declaration.EngineType is not UhtEngineType.Interface)
            {
                continue;
            }
            
            foreach (UhtFunction function in declaration.Functions)
            {
                if (!CanExportFunction(function))
                {
                    continue;
                }
                
                bool isOveridden = false;
                foreach (UhtFunction overridableFunction in overridableFunctions)
                {
                    if (function.SourceName == overridableFunction.SourceName)
                    {
                        isOveridden = true;
                        break;
                    }
                }
                
                if (isOveridden)
                {
                    continue;
                }
                
                overridableFunctions.Add(function);
            }
        }
    }
    
    public static void GetInterfaces(UhtClass classObj, ref List<UhtType> interfaces)
    {
        foreach (UhtStruct interfaceClass in classObj.Bases)
        {
            if (interfaceClass.EngineType != UhtEngineType.Interface)
            {
                continue;
            }
            
            interfaces.Add(interfaceClass);
        }
    }
    
    public static void GatherDependencies(UhtStruct typeObj, List<UhtFunction> functions, List<UhtFunction> overridableFunctions, List<UhtProperty> properties, List<UhtType> interfaces, List<string> dependencies)
    {
        foreach (UhtType @interface in interfaces)
        {
            dependencies.Add(GetNamespace(@interface));
        }

        if (typeObj.Super != null)
        {
            dependencies.Add(GetNamespace(typeObj.Super));
        }

        foreach (UhtFunction function in functions)
        {
            foreach (UhtType child in function.Children)
            {
                if (child is not UhtProperty property)
                {
                    continue;
                }
                
                GatherDependencies(property, dependencies);
            }
        }
        
        foreach (UhtFunction function in overridableFunctions)
        {
            foreach (UhtType child in function.Children)
            {
                if (child is not UhtProperty property)
                {
                    continue;
                }
                
                GatherDependencies(property, dependencies);
            }
        }
        
        foreach (UhtProperty property in properties)
        {
            GatherDependencies(property, dependencies);
        }
    }

    public static void GatherDependencies(UhtProperty property, List<string> dependencies)
    {
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property);
        List<UhtType> references = new List<UhtType>();
        translator.GetReferences(property, references);
        
        foreach (UhtType reference in references)
        {
            dependencies.Add(GetNamespace(reference));
        }
    }
}