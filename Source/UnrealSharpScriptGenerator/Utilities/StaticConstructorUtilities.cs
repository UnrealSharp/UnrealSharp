using System;
using System.Collections.Generic;
using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;

namespace UnrealSharpScriptGenerator.Utilities;

public static class StaticConstructorUtilities
{
    public static void ExportStaticConstructor(GeneratorStringBuilder GeneratorStringBuilder, UhtStruct structObj, List<UhtProperty> exportedProperties, List<UhtFunction> exportedFunctions, List<UhtFunction> overrides)
    {
        UhtClass? classObj = structObj as UhtClass;
        UhtScriptStruct? scriptStructObj = structObj as UhtScriptStruct;

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
            GeneratorStringBuilder.AppendLine("static readonly IntPtr NativeClassPtr;");
        }
        else
        {
            nativeClassPtrDeclaration = "IntPtr ";
        }

        if (scriptStructObj != null)
        {
            GeneratorStringBuilder.AppendLine("public static readonly int NativeDataSize;");
        }
        
        GeneratorStringBuilder.AppendLine($"static {structObj.EngineName}()");
        GeneratorStringBuilder.OpenBrace();
        
        string type = classObj != null ? "Class" : "Struct";
        GeneratorStringBuilder.AppendLine($"{nativeClassPtrDeclaration}NativeClassPtr = {ExporterCallbacks.CoreUObjectCallbacks}.CallGetNative{type}FromName(\"{structObj.EngineName}\");");
        
        ExportPropertiesStaticConstructor(GeneratorStringBuilder, exportedProperties);

        if (classObj != null)
        {
            ExportClassFunctionsStaticConstructor(GeneratorStringBuilder, exportedFunctions);
            ExportClassOverridesStaticConstructor(GeneratorStringBuilder, overrides);
        }
        else
        {
            GeneratorStringBuilder.AppendLine($"NativeDataSize = {ExporterCallbacks.UScriptStructCallbacks}.CallGetNativeStructSize(NativeClassPtr);");
        }
        
        GeneratorStringBuilder.CloseBrace();
    }
    
    public static void ExportClassFunctionsStaticConstructor(GeneratorStringBuilder GeneratorStringBuilder, List<UhtFunction> exportedFunctions)
    {
        foreach (UhtFunction function in exportedFunctions)
        {
            string functionName = function.SourceName;
            GeneratorStringBuilder.TryAddWithEditor(function);
            GeneratorStringBuilder.AppendLine($"{functionName}_NativeFunction = {ExporterCallbacks.UClassCallbacks}.CallGetNativeFunctionFromClassAndName(NativeClassPtr, \"{functionName}\");");
            
            if (function.HasParameters)
            {
                GeneratorStringBuilder.AppendLine($"{functionName}_ParamsSize = {ExporterCallbacks.UFunctionCallbacks}.CallGetNativeFunctionParamsSize({functionName}_NativeFunction);");
                
                foreach (UhtType parameter in function.Children)
                {
                    if (parameter is not UhtProperty property)
                    {
                        continue;
                    }
                
                    PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property);
                    translator.ExportParameterStaticConstructor(GeneratorStringBuilder, property, function, property.SourceName);
                }
            }
            GeneratorStringBuilder.TryEndWithEditor(function);
        }
    }
    
    public static void ExportClassOverridesStaticConstructor(GeneratorStringBuilder GeneratorStringBuilder, List<UhtFunction> overrides)
    {
        foreach (UhtFunction function in overrides)
        {
            if (!function.HasParameters)
            {
                continue;
            }
            
            GeneratorStringBuilder.TryAddWithEditor(function);
            string functionName = function.SourceName;
            
            GeneratorStringBuilder.AppendLine($"IntPtr {functionName}_NativeFunction = {ExporterCallbacks.UClassCallbacks}.CallGetNativeFunctionFromClassAndName(NativeClassPtr, \"{functionName}\");");
            GeneratorStringBuilder.AppendLine($"{functionName}_ParamsSize = {ExporterCallbacks.UFunctionCallbacks}.CallGetNativeFunctionParamsSize({functionName}_NativeFunction);");
            
            foreach (UhtType parameter in function.Children)
            {
                if (parameter is not UhtProperty property)
                {
                    continue;
                }
                
                PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property);
                translator.ExportParameterStaticConstructor(GeneratorStringBuilder, property, function, functionName);
            }
            
            GeneratorStringBuilder.TryEndWithEditor(function);
        }
    }

    public static void ExportPropertiesStaticConstructor(GeneratorStringBuilder GeneratorStringBuilder, List<UhtProperty> exportedProperties)
    {
        foreach (UhtProperty property in exportedProperties)
        {
            GeneratorStringBuilder.TryAddWithEditor(property);
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property);
            translator.ExportPropertyStaticConstructor(GeneratorStringBuilder, property, property.SourceName);
            GeneratorStringBuilder.TryEndWithEditor(property);
        }
    }
}