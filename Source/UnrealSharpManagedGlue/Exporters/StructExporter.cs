using EpicGames.UHT.Types;
using System.Collections.Generic;
using System.Text;
using UnrealSharpManagedGlue.Attributes;
using UnrealSharpManagedGlue.PropertyTranslators;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;
using UnrealSharpManagedGlue.Tooltip;

namespace UnrealSharpManagedGlue.Exporters;

public static class StructExporter
{
    public static void ExportStruct(UhtScriptStruct structObj, bool isManualExport)
    {
        GeneratorStringBuilder stringBuilder = new();
        List<UhtProperty> exportedProperties = new();
        Dictionary<UhtProperty, GetterSetterPair> getSetBackedProperties = new();
        List<UhtStruct> inheritanceHierarchy = new();
        UhtStruct? currentStruct = structObj;
        
        while (currentStruct is not null)
        {
            inheritanceHierarchy.Add(currentStruct);
            currentStruct = currentStruct.SuperStruct;
        }

        inheritanceHierarchy.Reverse();
        foreach (UhtStruct inheritance in inheritanceHierarchy)
        {
            ScriptGeneratorUtilities.GetExportedProperties(inheritance, exportedProperties, getSetBackedProperties);
        }
        
        // Check there are not properties with the same name, remove otherwise
        List<string> propertyNames = new();
        for (int i = 0; i < exportedProperties.Count; i++)
        {
            UhtProperty property = exportedProperties[i];
            string scriptName = property.GetParameterName();
            if (propertyNames.Contains(scriptName))
            {
                exportedProperties.RemoveAt(i);
                i--;
            }
            else
            {
                propertyNames.Add(scriptName);
            }
        }

        bool nullableEnabled = structObj.HasMetadata(UhtTypeUtilities.NullableEnable);
        bool isRecordStruct = structObj.HasMetadata("RecordStruct");
        bool isReadOnly = structObj.HasMetadata("ReadOnly");
        bool useProperties = structObj.HasMetadata("UseProperties");
        bool isBlittable = structObj.IsStructBlittable();
        bool isCopyable = structObj.IsStructNativelyCopyable();
        bool isDestructible = structObj.IsStructNativelyDestructible();
        bool isEquatable = structObj.IsStructEquatable(exportedProperties);
        
        stringBuilder.StartGlueFile(structObj, isBlittable, nullableEnabled);
                
        stringBuilder.AppendTooltip(structObj);
        
        AttributeBuilder attributeBuilder = new AttributeBuilder(structObj);
        
        if (isBlittable || isManualExport)
        {
            attributeBuilder.AddIsBlittableAttribute();
            attributeBuilder.AddStructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential);
        }
        
        attributeBuilder.AddGeneratedTypeAttribute(structObj);
        attributeBuilder.Finish();
        stringBuilder.AppendLine(attributeBuilder.ToString());

        string structName = structObj.GetStructName();
        List<string>? csInterfaces = null;

        if (isBlittable || !isManualExport) 
        { 
            csInterfaces = new List<string> { $"MarshalledStruct<{structName}>" };
            
            if (isDestructible) 
            {
                csInterfaces.Add("IDisposable");
            }
        }

        if (isEquatable)
        {
            // If null create the list and add the interface
            (csInterfaces ??= new()).Add($"IEquatable<{structName}>");
        }

        stringBuilder.DeclareType(structObj, isRecordStruct ? "record struct" : "struct", structName, csInterfaces: csInterfaces, modifiers: isReadOnly ? " readonly" : null);
        stringBuilder.AppendNativeTypePtr(structObj);

        if (isCopyable)
        {
            stringBuilder.AppendLine(isDestructible ? "private NativeStructHandle NativeHandle;" : "private byte[] Allocation;");
        }
        
        // For manual exports we just want to generate attributes
        if (!isManualExport)
        {
            List<string> reservedNames = GetReservedNames(exportedProperties);

            ExportStructProperties(structObj, stringBuilder, exportedProperties, isBlittable, reservedNames, isReadOnly, useProperties);
        }

        if (isBlittable)
        {
            StaticConstructorUtilities.ExportStaticConstructor(stringBuilder, structObj, 
                new List<UhtProperty>(), 
                new List<UhtFunction>(),
                new Dictionary<string, GetterSetterPair>(),
                new Dictionary<UhtProperty, GetterSetterPair>(),
                new List<UhtFunction>(), 
                true);
            
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"public static {structName} FromNative(IntPtr buffer) => BlittableMarshaller<{structName}>.FromNative(buffer, 0);");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"public void ToNative(IntPtr buffer) => BlittableMarshaller<{structName}>.ToNative(buffer, 0, this);");
        }
        else if (!isManualExport)
        {
            stringBuilder.AppendLine();
            StaticConstructorUtilities.ExportStaticConstructor(stringBuilder, structObj, exportedProperties, 
                new List<UhtFunction>(), 
                new Dictionary<string, GetterSetterPair>(), 
                new Dictionary<UhtProperty, GetterSetterPair>(),
                new List<UhtFunction>());
            
            stringBuilder.AppendLine();
            ExportMirrorStructMarshalling(stringBuilder, structObj, exportedProperties);

            if (isDestructible) 
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("public void Dispose()");
                stringBuilder.OpenBrace();
                stringBuilder.AppendLine("NativeHandle?.Dispose();");
                stringBuilder.CloseBrace();
            }
        }

        if (isEquatable)
        {
            ExportStructEquality(structName, stringBuilder, exportedProperties);
        }

        if (structObj.CanSupportArithmetic(exportedProperties))
        {
            ExportStructArithmetic(structName, stringBuilder, exportedProperties);
        }

        stringBuilder.CloseBrace();

        if (!isBlittable && !isManualExport)
        {
            ExportStructMarshaller(stringBuilder, structObj);
        }
        
        stringBuilder.EndGlueFile(structObj);
        FileExporter.SaveGlueToDisk(structObj, stringBuilder);
    }

    public static void ExportStructEquality(string structName, GeneratorStringBuilder stringBuilder, List<UhtProperty> exportedProperties)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("public override bool Equals(object? obj)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"return obj is {structName} other && Equals(other);");
        stringBuilder.CloseBrace();
        
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"public bool Equals({structName} other)");
        stringBuilder.OpenBrace();
        if (exportedProperties.Count == 0)
        {
            stringBuilder.AppendLine("return true;");
        }
        else
        {
            StringBuilder equalitySb = new StringBuilder();
            for (int i = 0; i < exportedProperties.Count; i++)
            {
                UhtProperty property = exportedProperties[i];
                string scriptName = property.GetPropertyName();
                equalitySb.Append($"this.{scriptName} == other.{scriptName}");
                if (i < exportedProperties.Count - 1)
                {
                    equalitySb.Append(" && ");
                }
            }
            stringBuilder.AppendLine($"return {equalitySb};");
        }
        stringBuilder.CloseBrace();
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("public override int GetHashCode()");
        stringBuilder.OpenBrace();
        
        if (exportedProperties.Count == 0)
        {
            stringBuilder.AppendLine("return 0;");
        }
        
        // More accurate hashcode equality
        else if (exportedProperties.Count <= 8)
        {
            StringBuilder hashSb = new StringBuilder();
            for (int i = 0; i < exportedProperties.Count; i++)
            {
                UhtProperty property = exportedProperties[i];
                string scriptName = property.GetPropertyName();
                hashSb.Append($"{scriptName}");
                if (i < exportedProperties.Count - 1)
                {
                    hashSb.Append(", ");
                }
            }

            stringBuilder.AppendLine($"return HashCode.Combine({hashSb});");
        }
        // Fallback to xor for more than 8 properties as HashCode.Combine only supports up to 8 parameters
        else
        {
            StringBuilder hashSb = new StringBuilder();
            for (int i = 0; i < exportedProperties.Count; i++)
            {
                UhtProperty property = exportedProperties[i];
                string scriptName = property.GetPropertyName();
                hashSb.Append($"{scriptName}.GetHashCode()");
                if (i < exportedProperties.Count - 1)
                {
                    hashSb.Append(" ^ ");
                }
            }

            stringBuilder.AppendLine($"return {hashSb};");
        }
        stringBuilder.CloseBrace();

        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"public static bool operator ==({structName} left, {structName} right)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine("return left.Equals(right);");
        stringBuilder.CloseBrace();

        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"public static bool operator !=({structName} left, {structName} right)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine("return !(left == right);");
        stringBuilder.CloseBrace();
    }

    public static void ExportStructArithmetic(string structName, GeneratorStringBuilder stringBuilder, List<UhtProperty> exportedProperties)
    {
        // Addition operator
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"public static {structName} operator +({structName} lhs, {structName} rhs)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"return new {structName}");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine();
        for (int i = 0; i < exportedProperties.Count; i++)
        {
            UhtProperty property = exportedProperties[i];
            PropertyTranslator translator = property.GetTranslator()!;

            translator.ExportPropertyArithmetic(stringBuilder, property, ArithmeticKind.Add);

            if (i < exportedProperties.Count - 1)
            {
                stringBuilder.Append(", ");
                stringBuilder.AppendLine();
            }
        }
        stringBuilder.UnIndent();
        stringBuilder.AppendLine("};");
        stringBuilder.CloseBrace();

        // Subtraction operator
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"public static {structName} operator -({structName} lhs, {structName} rhs)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"return new {structName}");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine();
        for (int i = 0; i < exportedProperties.Count; i++)
        {
            UhtProperty property = exportedProperties[i];
            PropertyTranslator translator = property.GetTranslator()!;

            translator.ExportPropertyArithmetic(stringBuilder, property, ArithmeticKind.Subtract);

            if (i < exportedProperties.Count - 1)
            {
                stringBuilder.Append(", ");
                stringBuilder.AppendLine();
            }
        }
        stringBuilder.UnIndent();
        stringBuilder.AppendLine("};");
        stringBuilder.CloseBrace();

        // Multiplication operator
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"public static {structName} operator *({structName} lhs, {structName} rhs)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"return new {structName}");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine();
        for (int i = 0; i < exportedProperties.Count; i++)
        {
            UhtProperty property = exportedProperties[i];
            PropertyTranslator translator = property.GetTranslator()!;

            translator.ExportPropertyArithmetic(stringBuilder, property, ArithmeticKind.Multiply);

            if (i < exportedProperties.Count - 1)
            {
                stringBuilder.Append(", ");
                stringBuilder.AppendLine();
            }
        }
        stringBuilder.UnIndent();
        stringBuilder.AppendLine("};");
        stringBuilder.CloseBrace();

        // Division operator
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"public static {structName} operator /({structName} lhs, {structName} rhs)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"return new {structName}");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine();
        for (int i = 0; i < exportedProperties.Count; i++)
        {
            UhtProperty property = exportedProperties[i];
            string scriptName = property.GetPropertyName();
            PropertyTranslator translator = property.GetTranslator()!;

            translator.ExportPropertyArithmetic(stringBuilder, property, ArithmeticKind.Divide);

            if (i < exportedProperties.Count - 1)
            {
                stringBuilder.Append(", ");
                stringBuilder.AppendLine();
            }
        }
        stringBuilder.UnIndent();
        stringBuilder.AppendLine("};");
        stringBuilder.CloseBrace();

        // Modulo operator
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"public static {structName} operator %({structName} lhs, {structName} rhs)");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine($"return new {structName}");
        stringBuilder.OpenBrace();
        stringBuilder.AppendLine();
        for (int i = 0; i < exportedProperties.Count; i++)
        {
            UhtProperty property = exportedProperties[i];
            PropertyTranslator translator = property.GetTranslator()!;

            translator.ExportPropertyArithmetic(stringBuilder, property, ArithmeticKind.Modulo);

            if (i < exportedProperties.Count - 1)
            {
                stringBuilder.Append(", ");
                stringBuilder.AppendLine();
            }
        }
        stringBuilder.UnIndent();
        stringBuilder.AppendLine("};");
        stringBuilder.CloseBrace();
    }
	
    public static void ExportStructProperties(UhtStruct structObj, GeneratorStringBuilder stringBuilder, List<UhtProperty> exportedProperties, bool suppressOffsets, List<string> reservedNames, bool isReadOnly, bool useProperties)
    {
        foreach (UhtProperty property in exportedProperties)
        {
            PropertyTranslator translator = property.GetTranslator()!;
            translator.ExportMirrorProperty(structObj, stringBuilder, property, suppressOffsets, reservedNames, isReadOnly, useProperties);
        }
    }
    
    public static List<string> GetReservedNames(List<UhtProperty> properties)
    {
        List<string> reservedNames = new();
        foreach (UhtProperty property in properties)
        {
            if (reservedNames.Contains(property.SourceName))
            {
                continue;
            }
            reservedNames.Add(property.SourceName);
        }
        return reservedNames;
    }

    public static void ExportStructMarshaller(GeneratorStringBuilder builder, UhtScriptStruct structObj)
    {
        string structName = structObj.GetStructName();
        
        builder.AppendLine();
        builder.AppendLine($"public static class {structName}Marshaller");
        builder.OpenBrace();
        
        builder.AppendLine($"public static {structName} FromNative(IntPtr nativeBuffer, int arrayIndex)");
        builder.OpenBrace();
        builder.AppendLine($"return new {structName}(nativeBuffer + (arrayIndex * GetNativeDataSize()));");
        builder.CloseBrace();
        
        builder.AppendLine();
        builder.AppendLine($"public static void ToNative(IntPtr nativeBuffer, int arrayIndex, {structName} obj)");
        builder.OpenBrace();
        builder.AppendLine($"obj.ToNative(nativeBuffer + (arrayIndex * GetNativeDataSize()));");
        builder.CloseBrace();

        builder.AppendLine();
        builder.AppendLine($"public static int GetNativeDataSize()");
        builder.OpenBrace();
        builder.AppendLine($"return {structName}.NativeDataSize;");
        builder.CloseBrace();
        builder.CloseBrace();
    }

    public static void ExportMirrorStructMarshalling(GeneratorStringBuilder builder, UhtScriptStruct structObj, List<UhtProperty> properties)
    {
        string structName = structObj.GetStructName();
        bool isCopyable = structObj.IsStructNativelyCopyable();
        bool isDestructible = structObj.IsStructNativelyDestructible();
        
        if (isCopyable)
        {
            builder.AppendLine();
            builder.AppendLine($"public {structName}()");
            builder.OpenBrace();
            builder.AppendLine(isDestructible
                ? "NativeHandle = new NativeStructHandle(NativeClassPtr);"
                : "Allocation = new byte[NativeDataSize];");
            builder.CloseBrace();
        }

        builder.AppendLine();
        builder.AppendLine("[System.Diagnostics.CodeAnalysis.SetsRequiredMembers]");
        builder.AppendLine($"public {structName}(IntPtr InNativeStruct)");
        builder.OpenBrace();
        builder.BeginUnsafeBlock();

        if (isCopyable)
        {
            if (isDestructible)
            {
                builder.AppendLine("NativeHandle = new NativeStructHandle(NativeClassPtr);");
                builder.AppendLine("fixed (NativeStructHandleData* StructDataPointer = &NativeHandle.Data)");
                builder.OpenBrace();
                builder.AppendLine($"IntPtr AllocationPointer = {ExporterCallbacks.UScriptStructCallbacks}.CallGetStructLocation(StructDataPointer, NativeClassPtr);");
            }
            else
            {
                builder.AppendLine("Allocation = new byte[NativeDataSize];");
                builder.AppendLine("fixed (byte* AllocationPointer = Allocation)");
                builder.OpenBrace();
            }
            
            builder.AppendLine($"{ExporterCallbacks.UScriptStructCallbacks}.CallNativeCopy(NativeClassPtr, InNativeStruct, (nint) AllocationPointer);");
            builder.CloseBrace();
        }
        else
        {
            foreach (UhtProperty property in properties)
            {
                PropertyTranslator translator = property.GetTranslator()!;
                string scriptName = property.GetPropertyName();
                string assignmentOrReturn = $"{scriptName} =";
                string offsetName = $"{property.SourceName}_Offset";
                builder.TryAddWithEditor(property);
                translator.ExportFromNative(builder, property, property.SourceName, assignmentOrReturn, "InNativeStruct", offsetName, false, false);
                builder.TryEndWithEditor(property);
            }
        }

        builder.EndUnsafeBlock();
        builder.CloseBrace();
        
        builder.AppendLine();
        builder.AppendLine($"public static {structName} FromNative(IntPtr buffer) => new {structName}(buffer);");
        
        builder.AppendLine();
        builder.AppendLine("public void ToNative(IntPtr buffer)");
        builder.OpenBrace();
        builder.BeginUnsafeBlock();
        
        if (structObj.IsStructNativelyCopyable())
        {
            if (structObj.IsStructNativelyDestructible())
            {
                builder.AppendLine("if (NativeHandle is null)");
                builder.OpenBrace();
                builder.AppendLine("NativeHandle = new NativeStructHandle(NativeClassPtr);");
                builder.CloseBrace();
                builder.AppendLine();
                builder.AppendLine("fixed (NativeStructHandleData* StructDataPointer = &NativeHandle.Data)");
                builder.OpenBrace();
                builder.AppendLine($"IntPtr AllocationPointer = {ExporterCallbacks.UScriptStructCallbacks}.CallGetStructLocation(StructDataPointer, NativeClassPtr);");
            }
            else
            {
                builder.AppendLine("if (Allocation is null)");
                builder.OpenBrace();
                builder.AppendLine("Allocation = new byte[NativeDataSize];");
                builder.AppendLine();
                builder.CloseBrace();
                builder.AppendLine("fixed (byte* AllocationPointer = Allocation)");
                builder.OpenBrace();
            }
            
            builder.AppendLine($"{ExporterCallbacks.UScriptStructCallbacks}.CallNativeCopy(NativeClassPtr, (nint) AllocationPointer, buffer);");
            builder.CloseBrace();
        }
        else
        {
            foreach (UhtProperty property in properties)
            {
                PropertyTranslator translator = property.GetTranslator()!;
                string scriptName = property.GetPropertyName();
                string offsetName = $"{property.SourceName}_Offset";
                builder.TryAddWithEditor(property);
                translator.ExportToNative(builder, property, property.SourceName, "buffer", offsetName, scriptName);
                builder.TryEndWithEditor(property);
            }
        }

        builder.EndUnsafeBlock();
        builder.CloseBrace();
    }
}