using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class OptionalPropertyTranslator : PropertyTranslator
{
    public OptionalPropertyTranslator() : base(ContainerSupportedUsages)
    {
        
    }
    
    public override bool CacheProperty => true;
    
    public override void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string propertyEngineName)
    {
        base.ExportPropertyVariables(builder, property, propertyEngineName);

        if (property.IsGenericType()) return;

        string wrapperType = GetMarshaller(property);
        if (property.IsOuter<UhtScriptStruct>())
        {
            builder.AppendLine($"static {wrapperType} {propertyEngineName}_Marshaller = null;");
        }
        else
        {
            builder.AppendLine($"{wrapperType} {propertyEngineName}_Marshaller = null;");
        }
    }
    
    public override void ExportPropertyGetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        var containerProperty = (UhtOptionalProperty) property;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(containerProperty.ValueProperty)!;
        
        string wrapperType = GetMarshaller(property);
        string marshallingDelegates = translator.ExportMarshallerDelegates(containerProperty.ValueProperty);

        builder.AppendLine($"{propertyManagedName}_Marshaller ??= new {wrapperType}({propertyManagedName}_NativeProperty, {marshallingDelegates});");
        builder.AppendLine($"return {propertyManagedName}_Marshaller.FromNative(IntPtr.Add(NativeObject, {propertyManagedName}_Offset), 0);");
    }
    
    public override void ExportParameterVariables(GeneratorStringBuilder builder, UhtFunction function,
        string nativeMethodName,
        UhtProperty property, string propertyEngineName)
    {
        base.ExportParameterVariables(builder, function, nativeMethodName, property, propertyEngineName);
        builder.AppendLine($"static IntPtr {nativeMethodName}_{propertyEngineName}_NativeProperty;");

        if (property.IsGenericType()) return;

        string wrapperType = GetMarshaller(property);
        if (function.FunctionFlags.HasAnyFlags(EFunctionFlags.Static))
        {
            builder.AppendLine($"static {wrapperType} {nativeMethodName}_{propertyEngineName}_Marshaller = null;");
        }
        else
        {
            builder.AppendLine($"{wrapperType} {nativeMethodName}_{propertyEngineName}_Marshaller = null;");
        }
    }

    public override void ExportParameterStaticConstructor(GeneratorStringBuilder builder, UhtProperty property,
        UhtFunction function, string propertyEngineName, string functionName)
    {
        base.ExportParameterStaticConstructor(builder, property, function, propertyEngineName, functionName);
        builder.AppendLine($"{functionName}_{propertyEngineName}_NativeProperty = {ExporterCallbacks.FPropertyCallbacks}.CallGetNativePropertyFromName({functionName}_NativeFunction, \"{propertyEngineName}\");");
    }
    
    public override void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string assignmentOrReturn,
        string sourceBuffer, string offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers)
    {
        UhtContainerBaseProperty containerProperty = (UhtContainerBaseProperty) property;
        
        UhtProperty valueProperty = containerProperty.ValueProperty;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(valueProperty)!;
        
        string nativeProperty = $"{propertyName}_NativeProperty";
        string marshaller = $"{propertyName}_Marshaller";

        if (property.Outer is UhtFunction function)
        {
            string nativeMethodName = function.SourceName;
            nativeProperty = $"{nativeMethodName}_{nativeProperty}";
            marshaller = $"{nativeMethodName}_{marshaller}";
        }
        
        string marshallerType = GetMarshaller(property);
        string marshallingDelegates = translator.ExportMarshallerDelegates(valueProperty);

        if (!reuseRefMarshallers)
        {
            if (property.IsGenericType())
            {
                builder.AppendLine($"var {marshaller} = new {marshallerType}({nativeProperty}, {marshallingDelegates});");
            }
            else
            {
                builder.AppendLine($"{marshaller} ??= new {marshallerType}({nativeProperty}, {marshallingDelegates});");
            }
            builder.AppendLine($"IntPtr {propertyName}_NativeBuffer = IntPtr.Add({sourceBuffer}, {offset});");
        }

        builder.AppendLine($"{assignmentOrReturn} {marshaller}.FromNative({propertyName}_NativeBuffer, 0);");
        
        if (bCleanupSourceBuffer)
        {
            ExportCleanupMarshallingBuffer(builder, property, propertyName);
        }
    }
    
    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer,
        string offset, string source)
    {
        UhtContainerBaseProperty containerProperty = (UhtContainerBaseProperty) property;
        UhtProperty valueProperty = containerProperty.ValueProperty;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(valueProperty)!;
        
        string nativeProperty = $"{propertyName}_NativeProperty";
        string marshaller = $"{propertyName}_Marshaller";

        if (property.Outer is UhtFunction function)
        {
            string nativeMethodName = function.SourceName;
            nativeProperty = $"{nativeMethodName}_{nativeProperty}";
            marshaller = $"{nativeMethodName}_{marshaller}";
        }
        
        string marshallerType = GetMarshaller(property);
        string marshallingDelegates = translator.ExportMarshallerDelegates(valueProperty);

        if (property.IsGenericType())
        {
            builder.AppendLine($"var {marshaller} = new {marshallerType}({nativeProperty}, {marshallingDelegates});");
        }
        else
        {
            builder.AppendLine($"{marshaller} ??= new {marshallerType}({nativeProperty}, {marshallingDelegates});");
        }
        builder.AppendLine($"IntPtr {propertyName}_NativeBuffer = IntPtr.Add({destinationBuffer}, {offset});");
        builder.AppendLine($"{marshaller}.ToNative({propertyName}_NativeBuffer, 0, {source});");
    }
    public override string ConvertCPPDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter)
    {
        throw new System.NotImplementedException();
    }

    public override string GetManagedType(UhtProperty property)
    {
        if (property.IsGenericType()) return "DOT";
        
        var optionalProperty = (UhtOptionalProperty)property;
        var translator = PropertyTranslatorManager.GetTranslator(optionalProperty.ValueProperty)!;
        return $"LanguageExt.Option<{translator.GetManagedType(optionalProperty.ValueProperty)}>";
    }

    public override string GetMarshaller(UhtProperty property)
    {
        if (property.Outer is UhtProperty outerProperty && outerProperty.IsGenericType())
        {
            return "OptionMarshaller<DOT>";
        }

        var optionalProperty = (UhtOptionalProperty)property;
        var translator = PropertyTranslatorManager.GetTranslator(optionalProperty.ValueProperty)!;
        return $"OptionMarshaller<{translator.GetManagedType(optionalProperty.ValueProperty)}>";
    }
    
    public override string ExportMarshallerDelegates(UhtProperty property)
    {
        throw new System.NotImplementedException();
    }
    
    public override string GetNullValue(UhtProperty property)
    {
        var optionalProperty = (UhtOptionalProperty)property;
        var translator = PropertyTranslatorManager.GetTranslator(optionalProperty.ValueProperty)!;
        return $"Optional.Empty<{translator.GetManagedType(optionalProperty.ValueProperty)}>()";
    }

    public override bool CanExport(UhtProperty property)
    {
        var containerProperty = (UhtOptionalProperty) property;
        var translator = PropertyTranslatorManager.GetTranslator(containerProperty.ValueProperty);
        return translator != null && translator.CanExport(containerProperty.ValueProperty) && translator.IsSupportedAsInner();
    }
    
    public override bool CanSupportGenericType(UhtProperty property) => true;
}