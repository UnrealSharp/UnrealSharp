using System;
using System.Collections.Generic;
using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class MapPropertyTranslator : PropertyTranslator
{
    public MapPropertyTranslator() : base(ContainerSupportedUsages)
    {
        
    }
    
    public override bool IsBlittable => false;
    public override bool NeedSetter => false;

    public override bool CanExport(UhtProperty property)
    {
        if (property is not UhtMapProperty mapProperty)
        {
            return false;
        }
        
        PropertyTranslator? keyTranslator = PropertyTranslatorManager.GetTranslator(mapProperty.KeyProperty);
        PropertyTranslator? valueTranslator = PropertyTranslatorManager.GetTranslator(mapProperty.ValueProperty);
        
        // These can be null apparently
        if (keyTranslator == null || valueTranslator == null)
        {
            return false;
        }
        
        return keyTranslator.IsSupportedAsInner() && valueTranslator.IsSupportedAsInner();
    }

    public override void GetReferences(UhtProperty property, List<UhtType> references)
    {
        base.GetReferences(property, references);
        UhtMapProperty mapProperty = (UhtMapProperty) property;
        references.Add(mapProperty.KeyProperty);
        references.Add(mapProperty.ValueProperty);
    }

    public override string GetManagedType(UhtProperty property)
    {
        UhtMapProperty mapProperty = (UhtMapProperty) property;
        PropertyTranslator keyTranslator = PropertyTranslatorManager.GetTranslator(mapProperty.KeyProperty);
        PropertyTranslator valueTranslator = PropertyTranslatorManager.GetTranslator(mapProperty.ValueProperty);
        
        string keyManagedType = keyTranslator.GetManagedType(mapProperty.KeyProperty);
        string valueManagedType = valueTranslator.GetManagedType(mapProperty.ValueProperty);
        
        string interfaceType = property.HasAnyFlags(EPropertyFlags.BlueprintReadOnly) ? "IReadOnlyDictionary" : "IDictionary";
        return $"System.Collections.Generic.{interfaceType}<{keyManagedType}, {valueManagedType}>";
    }

    public override void ExportPropertyStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        base.ExportPropertyStaticConstructor(builder, property, nativePropertyName);
        builder.AppendLine($"{nativePropertyName}_NativeProperty = {ExporterCallbacks.FPropertyCallbacks}.CallGetNativePropertyFromName(NativeClassPtr, \"{property.EngineName}\");");
    }

    public override void ExportParameterStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, UhtFunction function,
        string nativePropertyName)
    {
        base.ExportParameterStaticConstructor(builder, property, function, nativePropertyName);
        string nativeMethodName = function.SourceName;
        builder.AppendLine($"{nativeMethodName}_{nativePropertyName}_NativeProperty = {ExporterCallbacks.FPropertyCallbacks}.CallGetNativePropertyFromName({nativeMethodName}_NativeFunction, \"{property.EngineName}\");");
    }

    public override void ExportParameterVariables(GeneratorStringBuilder builder, UhtFunction function, string nativeMethodName,
        UhtProperty property, string nativePropertyName)
    {
        base.ExportParameterVariables(builder, function, nativeMethodName, property, nativePropertyName);

        string marshaller = GetMarshaller((UhtMapProperty) property);
        
        builder.AppendLine($"static IntPtr {nativeMethodName}_{nativePropertyName}_NativeProperty;");
        if (function.FunctionFlags.HasAnyFlags(EFunctionFlags.Static))
        {
            builder.AppendLine($"static {marshaller} {nativeMethodName}_{nativePropertyName}_Marshaller = null;");
        }
        else
        {
            builder.AppendLine($"{marshaller} {nativeMethodName}_{nativePropertyName}_Marshaller = null;");
        }
    }

    public override void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        base.ExportPropertyVariables(builder, property, nativePropertyName);
        builder.AppendLine($"static IntPtr {nativePropertyName}_NativeProperty;");
        
        string marshaller = GetMarshaller((UhtMapProperty) property);
        
        if (property.IsOuter<UhtScriptStruct>())
        {
            builder.AppendLine($"static {marshaller} {nativePropertyName}_Marshaller = null;");
        }
        else
        {
            builder.AppendLine($"{marshaller} {nativePropertyName}_Marshaller = null;");
        }
    }

    public override void ExportPropertyGetter(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        UhtMapProperty mapProperty = (UhtMapProperty) property;
        PropertyTranslator keyTranslator = PropertyTranslatorManager.GetTranslator(mapProperty.KeyProperty)!;
        PropertyTranslator valueTranslator = PropertyTranslatorManager.GetTranslator(mapProperty.ValueProperty)!;
        
        string keyMarshallingDelegates = keyTranslator.ExportMarshallerDelegates(mapProperty.KeyProperty);
        string valueMarshallingDelegates = valueTranslator.ExportMarshallerDelegates(mapProperty.ValueProperty);

        string marshaller = GetMarshaller(mapProperty);

        builder.AppendLine($"{nativePropertyName}_Marshaller ??= new {marshaller}({nativePropertyName}_NativeProperty, {keyMarshallingDelegates}, {valueMarshallingDelegates});");
        builder.AppendLine($"return {nativePropertyName}_Marshaller.FromNative(IntPtr.Add(NativeObject, {nativePropertyName}_Offset), 0);");
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string assignmentOrReturn,
        string sourceBuffer, string offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers)
    {
        UhtMapProperty mapProperty = (UhtMapProperty) property;
        PropertyTranslator keyTranslator = PropertyTranslatorManager.GetTranslator(mapProperty.KeyProperty);
        PropertyTranslator valueTranslator = PropertyTranslatorManager.GetTranslator(mapProperty.ValueProperty);
       
        string nativePropertyName = $"{propertyName}_NativeProperty";
        string marshaller = $"{propertyName}_Marshaller";

        if (property.Outer is UhtFunction function)
        {
            string nativeMethodName = function.SourceName;
            nativePropertyName = $"{nativeMethodName}_{nativePropertyName}";
            marshaller = $"{nativeMethodName}_{marshaller}";
        }
       
        string keyType = keyTranslator.GetManagedType(mapProperty.KeyProperty);
        string valueType = valueTranslator.GetManagedType(mapProperty.ValueProperty);
        string MarshallerType = $"MapCopyMarshaller<{keyType}, {valueType}>";

        if (!reuseRefMarshallers)
        {
            string KeyMarshallingDelegates = keyTranslator.ExportMarshallerDelegates(mapProperty.KeyProperty);
            string ValueMarshallingDelegates = valueTranslator.ExportMarshallerDelegates(mapProperty.ValueProperty);
       
            builder.AppendLine($"{marshaller} ??= new {MarshallerType}({nativePropertyName}, {KeyMarshallingDelegates}, {ValueMarshallingDelegates});");
            builder.AppendLine($"IntPtr {nativePropertyName}_ParamsBuffer = IntPtr.Add({sourceBuffer}, {offset});");
        }

        builder.AppendLine($"{assignmentOrReturn} {marshaller}.FromNative({sourceBuffer}, 0);");

        if (bCleanupSourceBuffer)
        {
            ExportCleanupMarshallingBuffer(builder, property, propertyName);
        }
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer,
        string offset, string source)
    {
       UhtMapProperty mapProperty = (UhtMapProperty) property;
       PropertyTranslator keyTranslator = PropertyTranslatorManager.GetTranslator(mapProperty.KeyProperty);
       PropertyTranslator valueTranslator = PropertyTranslatorManager.GetTranslator(mapProperty.ValueProperty);
       
       string nativePropertyName = $"{propertyName}_NativeProperty";
       string marshaller = $"{propertyName}_Marshaller";

       if (property.Outer is UhtFunction function)
       {
           string nativeMethodName = function.SourceName;
           nativePropertyName = $"{nativeMethodName}_{nativePropertyName}";
           marshaller = $"{nativeMethodName}_{marshaller}";
       }
       
       string keyType = keyTranslator.GetManagedType(mapProperty.KeyProperty);
       string valueType = valueTranslator.GetManagedType(mapProperty.ValueProperty);
       
       string MarshallerType = $"MapCopyMarshaller<{keyType}, {valueType}>";
       string KeyMarshallingDelegates = keyTranslator.ExportMarshallerDelegates(mapProperty.KeyProperty);
       string ValueMarshallingDelegates = valueTranslator.ExportMarshallerDelegates(mapProperty.ValueProperty);
       
       builder.AppendLine($"{marshaller} ??= new {MarshallerType}({nativePropertyName}, {KeyMarshallingDelegates}, {ValueMarshallingDelegates});");
       builder.AppendLine($"IntPtr {nativePropertyName}_NativeBuffer = IntPtr.Add({destinationBuffer}, {offset});");
       builder.AppendLine($"{marshaller}.ToNative({nativePropertyName}_NativeBuffer, 0, {source});");
    }

    public override string GetMarshaller(UhtProperty property)
    {
        throw new System.NotImplementedException();
    }

    public override string ExportMarshallerDelegates(UhtProperty property)
    {
        throw new System.NotImplementedException();
    }

    public override string GetNullValue(UhtProperty property)
    {
        return "null";
    }

    public override string ConvertCPPDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter)
    {
        throw new System.NotImplementedException();
    }

    private string GetMarshaller(UhtMapProperty property)
    {
        bool isStructProperty = property.IsOuter<UhtStruct>();
        bool isParameter = property.IsOuter<UhtFunction>();
        
        PropertyTranslator keyTranslator = PropertyTranslatorManager.GetTranslator(property.KeyProperty);
        PropertyTranslator valueTranslator = PropertyTranslatorManager.GetTranslator(property.ValueProperty);
        
        string marshallerType = isStructProperty || isParameter ? "MapCopyMarshaller" 
            : property.PropertyFlags.HasAnyFlags(EPropertyFlags.BlueprintReadOnly) ? "MapReadOnlyMarshaller" : "MapMarshaller";

        string keyType = keyTranslator.GetManagedType(property.KeyProperty);
        string valueType = valueTranslator.GetManagedType(property.ValueProperty);
        
        return $"{marshallerType}<{keyType}, {valueType}>";
    }
}