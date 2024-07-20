using System;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public static class DelegateExporter
{
    public static void ExportDelegate(UhtFunction function)
    {
        if (!function.HasAllFlags(EFunctionFlags.Delegate))
        {
            throw new Exception("Function is not a delegate");
        }
        
        GeneratorStringBuilder builder = new();
        
        string delegateName = DelegateBasePropertyTranslator.GetDelegateName(function);
        string delegateNamespace = function.GetNamespace();
        
        builder.GenerateTypeSkeleton(delegateNamespace);
        builder.AppendLine();
        
        string signatureName = $"{delegateName}.Signature";
        string superClass;
        if (function.HasAllFlags(EFunctionFlags.MulticastDelegate))
        {
            superClass =$"MulticastDelegate<{signatureName}>";
        }
        else
        {
            superClass = $"Delegate<{signatureName}>";
        }
        
        builder.DeclareType("class", delegateName, superClass);
        
        FunctionExporter.ExportDelegateFunction(builder, function);
        builder.AppendLine("static public void InitializeUnrealDelegate(IntPtr nativeDelegateProperty)");
        builder.OpenBrace();
        ExportDelegateFunctionStaticConstruction(builder, function);
        builder.CloseBrace();
        builder.CloseBrace();
        
        FileExporter.SaveGlueToDisk(function, builder);
    }

    private static void ExportDelegateFunctionStaticConstruction(GeneratorStringBuilder builder, UhtFunction function)
    {
        string delegateName = function.EngineName;
        builder.AppendLine($"{delegateName}_NativeFunction = FMulticastDelegatePropertyExporter.CallGetSignatureFunction(nativeDelegateProperty);");
        if (function.HasParameters)
        {
            builder.AppendLine($"{delegateName}_ParamsSize = {ExporterCallbacks.UFunctionCallbacks}.CallGetNativeFunctionParamsSize({delegateName}_NativeFunction);");
        }
        
        foreach (UhtProperty parameter in function.Properties)
        {
            PropertyTranslator propertyTranslator = PropertyTranslatorManager.GetTranslator(parameter)!;
            propertyTranslator.ExportParameterStaticConstructor(builder, parameter, function, parameter.EngineName, delegateName);
        }
    }
}