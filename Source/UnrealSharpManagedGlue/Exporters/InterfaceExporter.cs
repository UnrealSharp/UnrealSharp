using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Attributes;
using UnrealSharpManagedGlue.PropertyTranslators;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;
using UnrealSharpManagedGlue.Tooltip;

namespace UnrealSharpManagedGlue.Exporters;

public static class InterfaceExporter
{
    public static void ExportInterface(UhtClass interfaceObj)
    {
        GeneratorStringBuilder stringBuilder = new();
        
        bool nullableEnabled = interfaceObj.HasMetadata(UhtTypeUtilities.NullableEnable);
        string interfaceName = interfaceObj.GetStructName();
        
        stringBuilder.StartGlueFile(interfaceObj, nullableEnabled: nullableEnabled);
        stringBuilder.AppendTooltip(interfaceObj);
        
        AttributeBuilder attributeBuilder = new AttributeBuilder(interfaceObj);
        attributeBuilder.AddGeneratedTypeAttribute(interfaceObj);
        attributeBuilder.Finish();
        
        stringBuilder.AppendLine(attributeBuilder.ToString());
        stringBuilder.DeclareType(interfaceObj, "interface", interfaceName);
        stringBuilder.AppendNativeTypePtr(interfaceObj);
        
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"static {interfaceName} Wrap(UnrealSharp.CoreUObject.UObject obj)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"return new {interfaceName}Wrapper(obj);");
        stringBuilder.CloseBrace();
        
        List<UhtFunction> exportedFunctions = new();
        List<UhtFunction> exportedOverrides = new();
        Dictionary<string, GetterSetterPair> exportedGetterSetters = new();
        Dictionary<string, GetterSetterPair> getSetOverrides = new();

        if (interfaceObj.AlternateObject is UhtClass alternateObject)
        {
            ScriptGeneratorUtilities.GetExportedFunctions(alternateObject, exportedFunctions, exportedOverrides, exportedGetterSetters, getSetOverrides);
        }
        
        ScriptGeneratorUtilities.GetExportedFunctions(interfaceObj, exportedFunctions, exportedOverrides, exportedGetterSetters, getSetOverrides);
        
        ExportInterfaceProperties(stringBuilder, exportedGetterSetters);
        ExportInterfaceFunctions(stringBuilder, exportedFunctions);
        ExportInterfaceFunctions(stringBuilder, exportedOverrides);
        
        stringBuilder.CloseBrace();
        
        stringBuilder.AppendLine();
        
        stringBuilder.AppendLine($"internal sealed class {interfaceName}Wrapper : {interfaceName}, UnrealSharp.CoreUObject.IScriptInterface");
        stringBuilder.OpenBrace();
        
        stringBuilder.AppendNativeTypePtr(interfaceObj);
        
        stringBuilder.AppendLine("public UnrealSharp.CoreUObject.UObject Object { get; }");
        stringBuilder.AppendLine("private IntPtr NativeObject => Object.NativeObject;");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"internal {interfaceName}Wrapper(UnrealSharp.CoreUObject.UObject obj)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine("Object = obj;");
        stringBuilder.CloseBrace();

        StaticConstructorUtilities.ExportStaticConstructor(stringBuilder, interfaceObj,
            new List<UhtProperty>(),
            exportedFunctions,
            exportedGetterSetters,
            new Dictionary<UhtProperty, GetterSetterPair>(),
            exportedOverrides, 
            false, interfaceName + "Wrapper");
        
        ClassExporter.ExportCustomProperties(stringBuilder, exportedGetterSetters, new (), new ());
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
        
        stringBuilder.EndGlueFile(interfaceObj);
        FileExporter.SaveGlueToDisk(interfaceObj, stringBuilder);
    }

    static void ExportInterfaceProperties(GeneratorStringBuilder stringBuilder,
                                          Dictionary<string, GetterSetterPair> exportedGetterSetters)
    {
        foreach (var (_, getterSetterPair) in exportedGetterSetters)
        {
            if (getterSetterPair.Property is null)
            {
                throw new InvalidDataException("Properties should have a UProperty associated with them.");
            }
            
            UhtFunction firstAccessor = getterSetterPair.Accessors.First();
            UhtProperty firstProperty = getterSetterPair.Property;
            string propertyName = getterSetterPair.PropertyName;
            
            PropertyTranslator translator = firstProperty.GetTranslator()!;
            stringBuilder.TryAddWithEditor(firstAccessor);
            stringBuilder.AppendTooltip(firstProperty);
        
            string managedType = translator.GetManagedType(firstProperty);
            stringBuilder.AppendLine($"{managedType} {propertyName}");
            stringBuilder.OpenBrace();

            if (getterSetterPair.Getter is not null)
            {
                AppendPropertyFunctionDeclaration(stringBuilder, getterSetterPair.Getter);
                stringBuilder.AppendLine("get;");
            }

            if (getterSetterPair.Setter is not null)
            {
                AppendPropertyFunctionDeclaration(stringBuilder, getterSetterPair.Setter);
                stringBuilder.AppendLine("set;");
            }
        
            stringBuilder.CloseBrace();
            stringBuilder.TryEndWithEditor(firstAccessor);
        }
    }

    private static void AppendPropertyFunctionDeclaration(GeneratorStringBuilder stringBuilder, UhtFunction function)
    {
        AttributeBuilder attributeBuilder = new AttributeBuilder(function);
        
        if (function.FunctionFlags.HasAnyFlags(EFunctionFlags.BlueprintEvent))
        {
            attributeBuilder.AddArgument("FunctionFlags.BlueprintEvent");
        }
        
        attributeBuilder.AddGeneratedTypeAttribute(function);
        attributeBuilder.Finish();
        
        stringBuilder.AppendLine(attributeBuilder.ToString());
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
            FunctionType functionType = function.FunctionFlags.HasFlag(EFunctionFlags.BlueprintEvent) 
                ? FunctionType.BlueprintEvent 
                : FunctionType.Normal;
            
            FunctionExporter.ExportFunction(stringBuilder, function, functionType);
        }
    }
}