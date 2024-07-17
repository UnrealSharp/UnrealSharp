using System.Collections.Generic;
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
        stringBuilder.AppendLine("[UInterface]");
        stringBuilder.DeclareType("interface", interfaceName);

        stringBuilder.AppendLine($"public static readonly IntPtr NativeInterfaceClassPtr = UCoreUObjectExporter.CallGetNativeClassFromName(\"{interfaceName}\");");
        
        List<UhtFunction> exportedFunctions = new();
        List<UhtFunction> exportedOverrides = new();
        if (interfaceObj.AlternateObject is UhtClass alternateObject)
        {
            ScriptGeneratorUtilities.GetExportedFunctions(alternateObject, ref exportedFunctions, ref exportedOverrides);
        }
        
        ExportIntefaceFunctions(stringBuilder, exportedFunctions);
        
        stringBuilder.CloseBrace();

        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"public static class {interfaceName}Marshaller");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"public static void ToNative(IntPtr nativeBuffer, int arrayIndex, {interfaceName} obj)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine("	if (obj is CoreUObject.Object objectPointer)");
        stringBuilder.AppendLine("	{");
        stringBuilder.AppendLine("		InterfaceData data = new InterfaceData();");
        stringBuilder.AppendLine("		data.ObjectPointer = objectPointer.NativeObject;");
        stringBuilder.AppendLine($"		data.InterfacePointer = {interfaceName}.NativeInterfaceClassPtr;");
        stringBuilder.AppendLine("		BlittableMarshaller<InterfaceData>.ToNative(nativeBuffer, arrayIndex, data);");
        stringBuilder.AppendLine("	}");
        stringBuilder.CloseBrace();
        stringBuilder.AppendLine();

        stringBuilder.AppendLine($"public static {interfaceName} FromNative(IntPtr nativeBuffer, int arrayIndex)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine("	InterfaceData interfaceData = BlittableMarshaller<InterfaceData>.FromNative(nativeBuffer, arrayIndex);");
        stringBuilder.AppendLine("	CoreUObject.Object unrealObject = ObjectMarshaller<CoreUObject.Object>.FromNative(interfaceData.ObjectPointer, 0);");
        stringBuilder.AppendLine($"	return unrealObject as {interfaceName};");
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