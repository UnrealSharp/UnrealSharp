using System.Collections.Generic;
using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;

namespace UnrealSharpScriptGenerator.Utilities;

public static class StaticConstructorUtilities
{
    public static void ExportStaticConstructor(StringBuilder stringBuilder, UhtStruct structObj, List<UhtProperty> exportedProperties, List<UhtFunction> exportedFunctions, List<UhtFunction> overrides)
    {
        UhtClass? classObj = structObj as UhtClass;
        UhtScriptStruct? ScriptStructObj = structObj as UhtScriptStruct;

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
            stringBuilder.AppendLine("static readonly IntPtr NativeClassPtr;");
            nativeClassPtrDeclaration = "IntPtr ";
        }

        if (ScriptStructObj != null)
        {
            stringBuilder.AppendLine("public static readonly int NativeDataSize;");
        }
        
        string managedTypeName = ScriptGeneratorUtilities.GetCleanTypeName(structObj);
        
        stringBuilder.AppendLine($"static {managedTypeName}()");
        stringBuilder.OpenBrace();
        
        string type = classObj != null ? "class" : "struct";
        string cleanName = ScriptGeneratorUtilities.GetCleanTypeName(structObj);
        stringBuilder.AppendLine($"{nativeClassPtrDeclaration}NativeClassPtr = {ExporterCallbacks.CoreUObjectCallbacks}.CallGetNative{type}FromName(\"{cleanName}\");");
        
        ExportPropertiesStaticConstructor(stringBuilder, exportedProperties);

        if (classObj != null)
        {
            ExportClassFunctionsStaticConstructor(stringBuilder, exportedFunctions);
            ExportClassOverridesStaticConstructor(stringBuilder, overrides);
        }
        else
        {
            stringBuilder.AppendLine($"NativeDataSize = {ExporterCallbacks.UScriptStructCallbacks}.CallGetNativeStructSize(NativeClassPtr);");
        }
        
        stringBuilder.CloseBrace();
    }
    
    public static void ExportClassFunctionsStaticConstructor(StringBuilder stringBuilder, List<UhtFunction> exportedFunctions)
    {
        foreach (UhtFunction function in exportedFunctions)
        {
            string functionName = function.SourceName;
            stringBuilder.TryAddWithEditor(function);
            stringBuilder.AppendLine($"{functionName}_NativeFunction = {ExporterCallbacks.UClassCallbacks}.CallGetNativeFunctionFromClassAndName(NativeClassPtr, \"{functionName}\");");
            
            if (function.HasParameters)
            {
                stringBuilder.AppendLine($"{functionName}_ParamsSize = {ExporterCallbacks.UFunctionCallbacks}.CallGetNativeFunctionParamsSize({functionName}_NativeFunction);");
                
                foreach (UhtType parameter in function.Children)
                {
                    if (parameter is not UhtProperty property)
                    {
                        continue;
                    }
                
                    PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property);
                    translator.ExportParameterStaticConstructor(stringBuilder, property, function, functionName);
                }
            }
            
            stringBuilder.TryEndWithEditor(function);
        }
    }
    
    public static void ExportClassOverridesStaticConstructor(StringBuilder stringBuilder, List<UhtFunction> overrides)
    {
        foreach (UhtFunction function in overrides)
        {
            if (!function.HasParameters)
            {
                continue;
            }
            
            stringBuilder.TryAddWithEditor(function);
            string functionName = function.SourceName;
            
            stringBuilder.AppendLine($"IntPtr {functionName}_NativeFunction = {ExporterCallbacks.UClassCallbacks}.CallGetNativeFunctionFromClassAndName(NativeClassPtr, \"{functionName}\");");
            stringBuilder.AppendLine($"{functionName}_ParamsSize = {ExporterCallbacks.UFunctionCallbacks}.CallGetNativeFunctionParamsSize({functionName}_NativeFunction);");
            
            foreach (UhtType parameter in function.Children)
            {
                if (parameter is not UhtProperty property)
                {
                    continue;
                }
                
                PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property);
                translator.ExportParameterStaticConstructor(stringBuilder, property, function, functionName);
            }
            
            stringBuilder.TryEndWithEditor(function);
        }
    }

    public static void ExportPropertiesStaticConstructor(StringBuilder stringBuilder, List<UhtProperty> exportedProperties)
    {
        foreach (UhtProperty property in exportedProperties)
        {
            stringBuilder.TryAddWithEditor(property);
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property);
            translator.ExportPropertyStaticConstructor(stringBuilder, property, property.SourceName);
            stringBuilder.TryEndWithEditor(property);
        }
    }
}