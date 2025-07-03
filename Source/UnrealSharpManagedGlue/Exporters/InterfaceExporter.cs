using System;
using System.Collections.Generic;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Tooltip;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public static class InterfaceExporter
{
    public static void ExportInterface(UhtClass interfaceObj)
    {
        GeneratorStringBuilder stringBuilder = new();
        
        string interfaceName = interfaceObj.GetStructName();
        string typeNamespace = interfaceObj.GetNamespace();
        
        stringBuilder.GenerateTypeSkeleton(typeNamespace);
        stringBuilder.AppendTooltip(interfaceObj);
        
        AttributeBuilder attributeBuilder = new AttributeBuilder(interfaceObj);
        attributeBuilder.AddGeneratedTypeAttribute(interfaceObj);
        attributeBuilder.Finish();
        
        stringBuilder.AppendLine(attributeBuilder.ToString());
        stringBuilder.DeclareType(interfaceObj, "interface", interfaceName);
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"static {interfaceName} Wrap(UnrealSharp.CoreUObject.UObject obj)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"return new {interfaceName}Wrapper(obj);");
        stringBuilder.CloseBrace();
        
        List<UhtFunction> exportedFunctions = new();
        List<UhtFunction> exportedOverrides = new();
        Dictionary<string, GetterSetterPair> exportedGetterSetters = new();

        if (interfaceObj.AlternateObject is UhtClass alternateObject)
        {
            ScriptGeneratorUtilities.GetExportedFunctions(alternateObject, exportedFunctions, exportedOverrides, exportedGetterSetters);
        }
        
        ScriptGeneratorUtilities.GetExportedFunctions(interfaceObj, exportedFunctions, exportedOverrides, exportedGetterSetters);
        
        ExportInterfaceFunctions(stringBuilder, exportedFunctions);
        ExportInterfaceFunctions(stringBuilder, exportedOverrides);
        
        stringBuilder.CloseBrace();
        
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"internal sealed class {interfaceName}Wrapper : {interfaceName}, UnrealSharp.CoreUObject.IScriptInterface");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine("public UnrealSharp.CoreUObject.UObject Object { get; }");
        stringBuilder.AppendLine("private IntPtr NativeObject => Object.NativeObject;");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"internal {interfaceName}Wrapper(UnrealSharp.CoreUObject.UObject obj)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine("Object = obj;");
        stringBuilder.CloseBrace();
        
        ExportWrapperFunctions(stringBuilder, exportedFunctions);
        ExportWrapperFunctions(stringBuilder, exportedOverrides);
        
        stringBuilder.CloseBrace();
        

        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"public static class {interfaceName}Marshaller");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"public static void ToNative(IntPtr nativeBuffer, int arrayIndex, {interfaceName} obj)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"UnrealSharp.CoreUObject.ScriptInterfaceMarshaller<{interfaceName}>.ToNative(nativeBuffer, arrayIndex, obj);");
        stringBuilder.CloseBrace();
        stringBuilder.AppendLine();

        stringBuilder.AppendLine($"public static {interfaceName} FromNative(IntPtr nativeBuffer, int arrayIndex)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"return UnrealSharp.CoreUObject.ScriptInterfaceMarshaller<{interfaceName}>.FromNative(nativeBuffer, arrayIndex);");
        stringBuilder.CloseBrace();
        stringBuilder.CloseBrace();
        
        FileExporter.SaveGlueToDisk(interfaceObj, stringBuilder);
    }
    
    static void ExportInterfaceFunctions(GeneratorStringBuilder stringBuilder, List<UhtFunction> exportedFunctions)
    {
        foreach (UhtFunction function in exportedFunctions)
        {
            FunctionExporter.ExportInterfaceFunction(stringBuilder, function);
        }
    }
    
    static void ExportWrapperFunctions(GeneratorStringBuilder stringBuilder, List<UhtFunction> exportedFunctions)
    {
        foreach (UhtFunction function in exportedFunctions)
        {
            if (function.FunctionFlags.HasFlag(EFunctionFlags.BlueprintEvent))
            {
                FunctionExporter.ExportFunction(stringBuilder, function, FunctionType.BlueprintEvent);
            }
            else
            {
                FunctionExporter.ExportFunction(stringBuilder, function, FunctionType.Throwing);
            }
        }
    }
}