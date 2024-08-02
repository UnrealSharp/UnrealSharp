using System;
using System.Collections.Generic;
using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;

namespace UnrealSharpScriptGenerator.Utilities;

public static class StaticConstructorUtilities
{
    public static void ExportStaticConstructor(GeneratorStringBuilder generatorStringBuilder, UhtStruct structObj, List<UhtProperty> exportedProperties, List<UhtFunction> exportedFunctions, List<UhtFunction> overrides)
    {
        UhtClass? classObj = structObj as UhtClass;
        UhtScriptStruct? scriptStructObj = structObj as UhtScriptStruct;
        string structName = structObj.GetStructName();

        if (classObj != null && exportedProperties.Count == 0 && exportedFunctions.Count == 0 && overrides.Count == 0)
        {
            return;
        }

        bool hasStaticFunctions = false;
        foreach (UhtFunction function in exportedFunctions)
        {
            if (function.FunctionFlags.HasAnyFlags(EFunctionFlags.Static))
            {
                hasStaticFunctions = true;
                break;
            }
        }

        string nativeClassPtrDeclaration = string.Empty;
        if (hasStaticFunctions)
        {
            generatorStringBuilder.AppendLine("static readonly IntPtr NativeClassPtr;");
        }
        else
        {
            nativeClassPtrDeclaration = "IntPtr ";
        }

        if (scriptStructObj != null)
        {
            generatorStringBuilder.AppendLine("public static readonly int NativeDataSize;");
        }
        
        generatorStringBuilder.AppendLine($"static {structName}()");
        generatorStringBuilder.OpenBrace();
        
        string type = classObj != null ? "Class" : "Struct";
        generatorStringBuilder.AppendLine($"{nativeClassPtrDeclaration}NativeClassPtr = {ExporterCallbacks.CoreUObjectCallbacks}.CallGetNative{type}FromName(\"{structObj.EngineName}\");");
        
        ExportPropertiesStaticConstructor(generatorStringBuilder, exportedProperties);

        if (classObj != null)
        {
            ExportClassFunctionsStaticConstructor(generatorStringBuilder, exportedFunctions);
            ExportClassOverridesStaticConstructor(generatorStringBuilder, overrides);
        }
        else
        {
            generatorStringBuilder.AppendLine($"NativeDataSize = {ExporterCallbacks.UScriptStructCallbacks}.CallGetNativeStructSize(NativeClassPtr);");
        }
        
        generatorStringBuilder.CloseBrace();
    }
    
    public static void ExportClassFunctionsStaticConstructor(GeneratorStringBuilder generatorStringBuilder, List<UhtFunction> exportedFunctions)
    {
        foreach (UhtFunction function in exportedFunctions)
        {
            string functionName = function.SourceName;
            
            generatorStringBuilder.TryAddWithEditor(function);
            generatorStringBuilder.AppendLine($"{functionName}_NativeFunction = {ExporterCallbacks.UClassCallbacks}.CallGetNativeFunctionFromClassAndName(NativeClassPtr, \"{function.EngineName}\");");
            
            if (function.HasParametersOrReturnValue())
            {
                generatorStringBuilder.AppendLine($"{functionName}_ParamsSize = {ExporterCallbacks.UFunctionCallbacks}.CallGetNativeFunctionParamsSize({functionName}_NativeFunction);");
                
                foreach (UhtType parameter in function.Children)
                {
                    if (parameter is not UhtProperty property)
                    {
                        continue;
                    }

                    PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property)!;
                    translator.ExportParameterStaticConstructor(generatorStringBuilder, property, function, property.SourceName, functionName);
                }
            }
            generatorStringBuilder.TryEndWithEditor(function);
        }
    }
    
    public static void ExportClassOverridesStaticConstructor(GeneratorStringBuilder generatorStringBuilder, List<UhtFunction> overrides)
    {
        foreach (UhtFunction function in overrides)
        {
            if (!function.HasParametersOrReturnValue())
            {
                continue;
            }
            
            generatorStringBuilder.TryAddWithEditor(function);
            string functionName = function.SourceName;
            
            generatorStringBuilder.AppendLine($"IntPtr {functionName}_NativeFunction = {ExporterCallbacks.UClassCallbacks}.CallGetNativeFunctionFromClassAndName(NativeClassPtr, \"{function.EngineName}\");");
            generatorStringBuilder.AppendLine($"{functionName}_ParamsSize = {ExporterCallbacks.UFunctionCallbacks}.CallGetNativeFunctionParamsSize({functionName}_NativeFunction);");
            
            foreach (UhtType parameter in function.Children)
            {
                if (parameter is not UhtProperty property)
                {
                    continue;
                }
                
                PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property)!;
                translator.ExportParameterStaticConstructor(generatorStringBuilder, property, function, property.SourceName, functionName);
            }
            
            generatorStringBuilder.TryEndWithEditor(function);
        }
    }

    public static void ExportPropertiesStaticConstructor(GeneratorStringBuilder generatorStringBuilder, List<UhtProperty> exportedProperties)
    {
        foreach (UhtProperty property in exportedProperties)
        {
            generatorStringBuilder.TryAddWithEditor(property);
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property)!;
            translator.ExportPropertyStaticConstructor(generatorStringBuilder, property, property.SourceName);
            generatorStringBuilder.TryEndWithEditor(property);
        }
    }
}