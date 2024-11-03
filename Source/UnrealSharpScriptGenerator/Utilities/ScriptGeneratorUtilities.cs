using System;
using System.Collections.Generic;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;

namespace UnrealSharpScriptGenerator.Utilities;

public struct GetterSetterPair
{
    public UhtFunction? Getter;
    public UhtFunction? Setter;
    public string PropertyName;
}

public static class ScriptGeneratorUtilities
{
    public const string EngineNamespace = "UnrealSharp.Engine";
    public const string InteropNamespace = "UnrealSharp.Interop";
    public const string AttributeNamespace = "UnrealSharp.Attributes";
    
    public static string GetModuleName(UhtType typeObj)
    {
        if (typeObj.Outer is UhtPackage package)
        {
            return package.ShortName;
        }
        
        if (typeObj.Outer is UhtHeaderFile header)
        {
            return header.Package.ShortName;
        }

        return string.Empty;
    }
    
    public static string TryGetPluginDefine(string key)
    {
        Program.PluginModule.TryGetDefine(key, out string? generatedCodePath);
        return generatedCodePath!;
    }
    
    public static bool CanExportFunction(UhtFunction function)
    {
        if (function.HasAnyFlags(EFunctionFlags.Delegate | EFunctionFlags.MulticastDelegate))
        {
            return false;
        }

        return CanExportParameters(function);
    }
    
    public static bool CanExportParameters(UhtFunction function)
    {
        bool CanExportParameter(UhtProperty property, Func<PropertyTranslator, bool> isSupported)
        {
            PropertyTranslator? translator = PropertyTranslatorManager.GetTranslator(property);
            return translator != null && isSupported(translator) && translator.CanExport(property);
        }

        if (function.ReturnProperty != null && !CanExportParameter(function.ReturnProperty, translator => translator.IsSupportedAsReturnValue()))
        {
            return false;
        }

        foreach (UhtProperty parameter in function.Properties)
        {
            if (!CanExportParameter(parameter, translator => translator.IsSupportedAsParameter()))
            {
                return false;
            }
        }

        return true;
    }
    
    public static bool CanExportProperty(UhtProperty property)
    {
        PropertyTranslator? translator = PropertyTranslatorManager.GetTranslator(property);
        if (translator == null || !translator.CanExport(property))
        {
            return false;
        }

        bool isClassProperty = property.Outer!.EngineType == UhtEngineType.Class;
        bool canBeClassProperty = isClassProperty && translator.IsSupportedAsProperty();
        bool canBeStructProperty = !isClassProperty && translator.IsSupportedAsStructProperty();
        return canBeClassProperty || canBeStructProperty;
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
    
    public static void GetExportedProperties(UhtStruct structObj, List<UhtProperty> properties)
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
    
    public static bool IsPackagePartOfEngine(this UhtPackage package)
    {
        return package.IsPartOfEngine || package.Module == Program.Factory.PluginModule;
    }
    
    public static void GetExportedFunctions(UhtClass classObj, 
        List<UhtFunction> functions, 
         List<UhtFunction> overridableFunctions, 
        Dictionary<string, GetterSetterPair> getterSetterPairs)
    {
        foreach (UhtFunction function in classObj.Functions)
        {
            if (!CanExportFunction(function))
            {
                continue;
            }
            
            if (function.IsAnyGetter() || function.IsAnySetter())
            {
                continue;
            }
            
            if ((function.SourceName.StartsWith("Get") && function.ReturnProperty != null && !function.HasParameters) 
                || (function.SourceName.StartsWith("Set") && function.Properties.Count() == 1 && !function.Properties.First().HasAllFlags(EPropertyFlags.OutParm | EPropertyFlags.ReferenceParm)))
            {
                string propertyName;
                if (function.SourceName.Length > 3)
                {
                    propertyName = function.SourceName.Substring(3);
                }
                else
                {
                    propertyName = function.SourceName;
                }

                propertyName = NameMapper.EscapeKeywords(propertyName);

                UhtFunction? sameNameFunction = classObj.FindFunctionByName(propertyName);
                if (sameNameFunction != null && sameNameFunction != function)
                {
                    continue;
                }
            
                bool ComparePropertyName(UhtProperty arg1, string arg2)
                {
                    return arg1.SourceName == arg2 || arg1.GetPropertyName() == arg2;
                }
            
                UhtProperty classProperty = classObj.FindPropertyByName(propertyName, ComparePropertyName);
                UhtProperty firstProperty = function.ReturnProperty != null ? function.ReturnProperty : function.Properties.First();
                if (classProperty != null && (!classProperty.IsSameType(firstProperty) || classProperty.HasAnyGetter() || classProperty.HasAnySetter()))
                {
                    continue;
                }
                
                if (!getterSetterPairs.TryGetValue(propertyName, out GetterSetterPair pair))
                {
                    pair = new GetterSetterPair
                    {
                        PropertyName = propertyName
                    };
                    
                    getterSetterPairs[propertyName] = pair;
                }

                bool IsOutParm(UhtProperty property)
                {
                    return property.HasAllFlags(EPropertyFlags.OutParm) && !property.HasAllFlags(EPropertyFlags.ConstParm);
                }

                if (function.ReturnProperty != null || function.Properties.Any(IsOutParm))
                {
                    pair.Getter = function;
                }
                else
                {
                    pair.Setter = function;
                }
                
                getterSetterPairs[propertyName] = pair;
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

        bool HasFunction(List<UhtFunction> functions, UhtFunction functionToTest)
        {
            foreach (UhtFunction function in functions)
            {
                if (function.SourceName == functionToTest.SourceName || function.CppImplName == functionToTest.CppImplName)
                {
                    return true;
                }
            }
            return false;
        }
        
        foreach (UhtStruct declaration in classObj.Bases)
        {
            if (declaration.EngineType is not (UhtEngineType.Interface or UhtEngineType.NativeInterface))
            {
                continue;
            }
            
            if (declaration.AlternateObject is not UhtClass interfaceClass)
            {
                continue;
            }
            
            foreach (UhtFunction function in interfaceClass.Functions)
            {
                if (HasFunction(functions, function) || HasFunction(overridableFunctions, function))
                {
                    continue;
                }
                
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
        }
    }
    
    public static List<UhtClass> GetInterfaces(this UhtClass classObj)
    {
        List<UhtClass> interfaces = new();
        foreach (UhtStruct interfaceClass in classObj.Bases)
        {
            UhtEngineType engineType = interfaceClass.EngineType;
            if (engineType is UhtEngineType.Interface or UhtEngineType.NativeInterface)
            {
                interfaces.Add((UhtClass) interfaceClass);
            }
        }
        
        return interfaces;
    }
}