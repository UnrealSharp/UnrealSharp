using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class ArrayPropertyTranslator : PropertyTranslator
{
    public ArrayPropertyTranslator() : base(ContainerSupportedUsages)
    {
        
    }

    public override bool IsBlittable => false;
    public override bool NeedSetter => false;

    public override bool CanExport(UhtProperty property)
    {
        UhtArrayProperty arrayProperty = (UhtArrayProperty) property;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(arrayProperty.ValueProperty);
        return translator != null && translator.CanExport(arrayProperty.ValueProperty);
    }

    public override string GetManagedType(UhtProperty property)
    {
        return GetWrapperInterface(property);
    }

    public override string GetMarshaller(UhtProperty property)
    {
        throw new System.NotImplementedException();
    }

    public override string ExportMarshallerDelegates(UhtProperty property)
    {
        throw new System.NotImplementedException();
    }

    public override void ExportPropertyGetter(StringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        UhtArrayProperty arrayProperty = (UhtArrayProperty) property;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(arrayProperty.ValueProperty);
        
        string wrapperType = GetWrapperType(property);
        string marshallingDelegates = translator.ExportMarshallerDelegates(arrayProperty.ValueProperty);

        builder.AppendLine($"{nativePropertyName}_Marshaller ??= new {wrapperType}(1, {nativePropertyName}_NativeProperty, {marshallingDelegates});");
        builder.AppendLine($"return {nativePropertyName}_Marshaller.FromNative(IntPtr.Add(NativeObject, {nativePropertyName}_Offset), 0);");
    }

    public override void ExportPropertyVariables(StringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        base.ExportPropertyVariables(builder, property, nativePropertyName);
        builder.AppendLine($"static IntPtr {nativePropertyName}_NativeProperty;");

        string wrapperType = GetWrapperType(property);
        if (property.IsOuter<UhtScriptStruct>())
        {
            builder.AppendLine($"static {wrapperType} {nativePropertyName}_Marshaller = null;");
        }
        else
        {
            builder.AppendLine($"{wrapperType} {nativePropertyName}_Marshaller = null;");
        }
    }

    public override void ExportParameterVariables(StringBuilder builder, UhtFunction function, string nativeMethodName,
        UhtProperty property, string nativePropertyName)
    {
        base.ExportParameterVariables(builder, function, nativeMethodName, property, nativePropertyName);
        builder.AppendLine($"static IntPtr {nativeMethodName}_{nativePropertyName}_NativeProperty;");
        
        string wrapperType = GetWrapperType(property);
        if (function.FunctionFlags.HasAnyFlags(EFunctionFlags.Static))
        {
            builder.AppendLine($"static {wrapperType} {nativeMethodName}_{nativePropertyName}_Marshaller = null;");
        }
        else
        {
            builder.AppendLine($"{wrapperType} {nativeMethodName}_{nativePropertyName}_Marshaller = null;");
        }
    }

    public override void ExportParameterStaticConstructor(StringBuilder builder, UhtProperty property, UhtFunction function,
        string nativePropertyName)
    {
        base.ExportParameterStaticConstructor(builder, property, function, nativePropertyName);
        builder.AppendLine($"{nativePropertyName}_{property.SourceName}_NativeProperty = {ExporterCallbacks.FPropertyCallbacks}.CallGetPropertyFromName({function.SourceName}_NativeFunction, \"{nativePropertyName}\");");
    }

    public override void ExportPropertyStaticConstructor(StringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        base.ExportPropertyStaticConstructor(builder, property, nativePropertyName);
        builder.AppendLine($"{nativePropertyName}_NativeProperty = {ExporterCallbacks.FPropertyCallbacks}.CallGetNativePropertyFromName(NativeClassPtr, \"{nativePropertyName}\");");
    }

    public override string GetNullValue(UhtProperty property)
    {
        return "null";
    }
    
    public override string ConvertCPPDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter)
    {
        throw new System.NotImplementedException();
    }
    
    public override void ExportFromNative(StringBuilder builder, UhtProperty property, string propertyName, string assignmentOrReturn,
        string sourceBuffer, string offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers)
    {
        UhtArrayProperty arrayProperty = (UhtArrayProperty) property;
        UhtProperty valueProperty = arrayProperty.ValueProperty;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(valueProperty);
        
        string nativeProperty = $"{propertyName}_NativeProperty";
        string marshaller = $"{propertyName}_Marshaller";

        if (property.Outer is UhtFunction function)
        {
            string nativeMethodName = function.SourceName;
            nativeProperty = $"{nativeMethodName}_{propertyName}";
            marshaller = $"{nativeMethodName}_Marshaller";
        }
        
        string innerType = translator.GetManagedType(valueProperty);
        string marshallerType = $"ArrayCopyMarshaller<{innerType}>";
        string marshallingDelegates = translator.ExportMarshallerDelegates(valueProperty);

        if (!reuseRefMarshallers)
        {
            builder.AppendLine($"{marshaller} ??= new {marshallerType}({nativeProperty}, {marshallingDelegates});");
            builder.AppendLine($"IntPtr {nativeProperty}_NativeBuffer = IntPtr.Add({sourceBuffer}, {offset});");
        }

        builder.AppendLine($"{assignmentOrReturn} {marshaller}.FromNative({propertyName}_NativeBuffer, 0);");
        
        if (bCleanupSourceBuffer)
        {
            ExportCleanupMarshallingBuffer(builder, property, propertyName);
        }
    }
    
    public override void ExportToNative(StringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer,
        string offset, string source)
    {
        UhtArrayProperty arrayProperty = (UhtArrayProperty) property;
        UhtProperty valueProperty = arrayProperty.ValueProperty;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(valueProperty);
        
        string nativeProperty = $"{propertyName}_NativeProperty";
        string marshaller = $"{propertyName}_Marshaller";

        if (property.Outer is UhtFunction function)
        {
            string nativeMethodName = function.SourceName;
            nativeProperty = $"{nativeMethodName}_{propertyName}";
            marshaller = $"{nativeMethodName}_Marshaller";
        }
        
        string innerType = translator.GetManagedType(valueProperty);
        string marshallerType = $"ArrayCopyMarshaller<{innerType}>";
        
        string marshallingDelegates = translator.ExportMarshallerDelegates(valueProperty);
        builder.AppendLine($"{marshaller} ??= new {marshallerType}({nativeProperty}, {marshallingDelegates});");
        builder.AppendLine($"IntPtr {nativeProperty}_NativeBuffer = IntPtr.Add({destinationBuffer}, {offset});");
        builder.AppendLine($"{marshaller}.ToNative({nativeProperty}_NativeBuffer, 0, {source});");
    }

    public override void ExportCleanupMarshallingBuffer(StringBuilder builder, UhtProperty property, string paramName)
    {
        UhtFunction function = (UhtFunction) property.Outer;
        string marshaller = $"{function.SourceName}_{paramName}_Marshaller";
        builder.AppendLine($"{marshaller}.DestructInstance({paramName}_NativeBuffer, 0);");
    }

    private string GetWrapperType(UhtProperty property)
    {
        bool isStructProperty = property.IsOuter<UhtStruct>();
        bool isParameter = property.IsOuter<UhtFunction>();
        UhtArrayProperty arrayProperty = (UhtArrayProperty) property;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(arrayProperty.ValueProperty);
        string ArrayType = isStructProperty || isParameter ? "ArrayCopyMarshaller" 
            : property.PropertyFlags.HasAnyFlags(EPropertyFlags.BlueprintReadOnly) ? "ArrayReadOnlyMarshaller" : "ArrayMarshaller";

        return $"{ArrayType}<{translator.GetManagedType(arrayProperty.ValueProperty)}>";
    }

    private string GetWrapperInterface(UhtProperty property)
    {
        UhtArrayProperty arrayProperty = (UhtArrayProperty) property;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(arrayProperty.ValueProperty);
        string innerManagedType = translator.GetManagedType(arrayProperty.ValueProperty);
        string interfaceType = property.PropertyFlags.HasAnyFlags(EPropertyFlags.BlueprintReadOnly) ? "IReadOnlyList" : "IList";
        return $"System.Collection.Generic.{interfaceType}<{innerManagedType}>";
    }
}