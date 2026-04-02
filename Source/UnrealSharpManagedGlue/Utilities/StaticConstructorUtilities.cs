using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.PropertyTranslators;
using UnrealSharpManagedGlue.SourceGeneration;

namespace UnrealSharpManagedGlue.Utilities;

public static class StaticConstructorUtilities
{
    public static void ExportStaticConstructor(GeneratorStringBuilder generatorStringBuilder, 
        UhtStruct structObj, 
        List<UhtProperty> exportedProperties, 
        List<UhtFunction> exportedFunctions,
        Dictionary<string, GetterSetterPair> exportedGetterSetters,
        Dictionary<UhtProperty, GetterSetterPair> getSetBackedProperties,
        List<UhtFunction> overrides,
        bool isBlittable = false, string? customStaticConstructorName = null)
    {
        UhtClass? classObj = structObj as UhtClass;
        UhtScriptStruct? scriptStructObj = structObj as UhtScriptStruct;
        string structName = structObj.GetStructName();

        if (classObj != null && exportedProperties.Count == 0 
                             && exportedFunctions.Count == 0 
                             && overrides.Count == 0 
                             && exportedGetterSetters.Count == 0 
                             && getSetBackedProperties.Count == 0)
        {
            return;
        }
        
        if (scriptStructObj != null)
        {
            if (classObj == null)
            {
                generatorStringBuilder.AppendLine("public static IntPtr GetNativeClassPtr() => NativeClassPtr;");
            }
            
            if (isBlittable)
            {
                generatorStringBuilder.AppendLine("public static int GetNativeDataSize()");
                generatorStringBuilder.OpenBrace();
                generatorStringBuilder.BeginUnsafeBlock();
                generatorStringBuilder.AppendLine($"return sizeof({structName});");
                generatorStringBuilder.EndUnsafeBlock();
                generatorStringBuilder.CloseBrace();
            }
            else
            {
                generatorStringBuilder.AppendLine("public static readonly int NativeDataSize;");
                if (classObj == null)
                {
                    generatorStringBuilder.AppendLine("public static int GetNativeDataSize() => NativeDataSize;");
                }
            }
        }
        
        string staticCtorName = customStaticConstructorName != null ? customStaticConstructorName : structName;
        generatorStringBuilder.AppendLine($"static {staticCtorName}()");
        generatorStringBuilder.OpenBrace();
        
        ExportPropertiesStaticConstructor(generatorStringBuilder, exportedProperties);
        ExportGetSetBackedPropertyStaticConstructor(generatorStringBuilder, getSetBackedProperties);

        if (classObj != null)
        {
            foreach (KeyValuePair<string, GetterSetterPair> pair in exportedGetterSetters)
            {
                if (pair.Value.Getter != null)
                {
                    ExportClassFunctionStaticConstructor(generatorStringBuilder, pair.Value.Getter);
                }
                
                if (pair.Value.Setter != null)
                {
                    ExportClassFunctionStaticConstructor(generatorStringBuilder, pair.Value.Setter);
                }
            }
            
            ExportClassFunctionsStaticConstructor(generatorStringBuilder, exportedFunctions);
            ExportClassOverridesStaticConstructor(generatorStringBuilder, overrides);
        }
        else if (!isBlittable)
        {
            generatorStringBuilder.AppendLine($"NativeDataSize = {ExporterCallbacks.UScriptStructCallbacks}.CallGetNativeStructSize(NativeClassPtr);");
        }
        
        generatorStringBuilder.CloseBrace();
    }
    
    public static void ExportClassFunctionsStaticConstructor(GeneratorStringBuilder generatorStringBuilder, List<UhtFunction> exportedFunctions)
    {
        foreach (UhtFunction function in exportedFunctions)
        {
            ExportClassFunctionStaticConstructor(generatorStringBuilder, function);
        }
    }
    
    public static void ExportClassFunctionStaticConstructor(GeneratorStringBuilder generatorStringBuilder, UhtFunction function)
    {
        string functionName = function.SourceName;

        string nativeFunctionName = function.GetNativeFunctionName();
            
        generatorStringBuilder.TryAddWithEditor(function);
        generatorStringBuilder.AppendLine($"{nativeFunctionName} = {ExporterCallbacks.UClassCallbacks}.CallGetNativeFunctionFromClassAndName(NativeClassPtr, \"{function.EngineName}\");");
            
        if (function.HasParametersOrReturnValue())
        {
            bool hasCustomStructParams = function.HasCustomStructParamSupport();
            string variableName = hasCustomStructParams ? $"{functionName}_NativeParamsSize" : $"{functionName}_ParamsSize";
            generatorStringBuilder.AppendLine($"{variableName} = {ExporterCallbacks.UFunctionCallbacks}.CallGetNativeFunctionParamsSize({functionName}_NativeFunction);");
                
            foreach (UhtType parameter in function.Children)
            {
                if (parameter is not UhtProperty property)
                {
                    continue;
                }

                PropertyTranslator translator = property.GetTranslator()!;
                translator.ExportParameterStaticConstructor(generatorStringBuilder, property, function, property.SourceName, functionName);
            }
            
            if (hasCustomStructParams)
            {
                List<string> customStructParams = function.GetCustomStructParams();
                List<string> initializerElements = customStructParams.ConvertAll(param => $"CallGetNativePropertyFromName({nativeFunctionName}, \"{param}\")");
                generatorStringBuilder.AppendLine($"{functionName}_CustomStructureNativeProperties = new IntPtr[]{{{string.Join(", ", initializerElements)}}};");
            }
        }
        generatorStringBuilder.TryEndWithEditor(function);
    }
    
    public static void ExportClassOverridesStaticConstructor(GeneratorStringBuilder generatorStringBuilder, List<UhtFunction> overrides)
    {
        foreach (UhtFunction function in overrides)
        {
            generatorStringBuilder.TryAddWithEditor(function);
            string functionName = function.SourceName;
            
            string intPtrDeclaration = function.IsBlueprintImplementableEvent() ? "IntPtr " : "";
            generatorStringBuilder.AppendLine($"{intPtrDeclaration}{functionName}_NativeFunction = {ExporterCallbacks.UClassCallbacks}.CallGetNativeFunctionFromClassAndName(NativeClassPtr, \"{function.EngineName}\");");
            
            if (function.HasParametersOrReturnValue())
            {
                generatorStringBuilder.AppendLine($"{functionName}_ParamsSize = {ExporterCallbacks.UFunctionCallbacks}.CallGetNativeFunctionParamsSize({functionName}_NativeFunction);");
            
                foreach (UhtType parameter in function.Children)
                {
                    if (parameter is not UhtProperty property)
                    {
                        continue;
                    }
                
                    PropertyTranslator translator = property.GetTranslator()!;
                    translator.ExportParameterStaticConstructor(generatorStringBuilder, property, function, property.SourceName, functionName);
                }
            }
            
            generatorStringBuilder.TryEndWithEditor(function);
        }
    }

    public static void ExportPropertiesStaticConstructor(GeneratorStringBuilder generatorStringBuilder, List<UhtProperty> exportedProperties)
    {
        foreach (UhtProperty property in exportedProperties)
        {
            ExportPropertyStaticConstructor(generatorStringBuilder, property);
        }
    }
    
    public static void ExportGetSetBackedPropertyStaticConstructor(GeneratorStringBuilder generatorStringBuilder, Dictionary<UhtProperty, GetterSetterPair> getSetBackedProperties)
    {
        foreach (KeyValuePair<UhtProperty, GetterSetterPair> pair in getSetBackedProperties)
        {
            ExportPropertyStaticConstructor(generatorStringBuilder, pair.Key);
        }
    }
    
    private static void ExportPropertyStaticConstructor(GeneratorStringBuilder generatorStringBuilder, UhtProperty property)
    {
        generatorStringBuilder.TryAddWithEditor(property);
        PropertyTranslator translator = property.GetTranslator()!;
        translator.ExportPropertyStaticConstructor(generatorStringBuilder, property, property.SourceName);
        generatorStringBuilder.TryEndWithEditor(property);
    }
}