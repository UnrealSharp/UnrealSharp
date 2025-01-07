using System;
using System.Collections.Generic;
using System.Linq;
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
        
        List<UhtFunction> exportedFunctions = new();
        List<UhtFunction> exportedOverrides = new();
        Dictionary<string, GetterSetterPair> exportedGetterSetters = new();

        if (interfaceObj.AlternateObject is UhtClass alternateObject)
        {
            ScriptGeneratorUtilities.GetExportedFunctions(alternateObject, exportedFunctions, exportedOverrides, exportedGetterSetters);
        }
        
        ScriptGeneratorUtilities.GetExportedFunctions(interfaceObj, exportedFunctions, exportedOverrides, exportedGetterSetters);
        
        ExportIntefaceFunctions(stringBuilder, exportedFunctions);
        ExportIntefaceFunctions(stringBuilder, exportedOverrides);
        
        stringBuilder.CloseBrace();

        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"public static class {interfaceName}Marshaller");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"static readonly IntPtr NativeInterfaceClassPtr = UCoreUObjectExporter.CallGetNativeClassFromName(\"{interfaceObj.EngineName}\");");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"public static void ToNative(IntPtr nativeBuffer, int arrayIndex, {interfaceName} obj)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"UnrealSharp.CoreUObject.ScriptInterfaceMarshaller<{interfaceName}>.ToNative(nativeBuffer, arrayIndex, obj, NativeInterfaceClassPtr);");
        stringBuilder.CloseBrace();
        stringBuilder.AppendLine();

        stringBuilder.AppendLine($"public static {interfaceName} FromNative(IntPtr nativeBuffer, int arrayIndex)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"return UnrealSharp.CoreUObject.ScriptInterfaceMarshaller<{interfaceName}>.FromNative(nativeBuffer, arrayIndex);");
        stringBuilder.CloseBrace();
        stringBuilder.CloseBrace();
        
        FileExporter.SaveGlueToDisk(interfaceObj, stringBuilder);
    }
    
    static void ExportIntefaceFunctions(GeneratorStringBuilder stringBuilder, List<UhtFunction> exportedFunctions)
    {
        foreach (UhtFunction function in exportedFunctions)
        {
            FunctionExporter.ExportInterfaceFunction(stringBuilder, function);
        }
    }
}