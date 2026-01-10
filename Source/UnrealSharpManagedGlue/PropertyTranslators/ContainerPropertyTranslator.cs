using System.Collections.Generic;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class ContainerPropertyTranslator : PropertyTranslator
{
    private readonly string _copyMarshallerName;
    private readonly string _readOnlyMarshallerName;
    private readonly string _marshallerName;
    
    private readonly string _readOnlyInterfaceName;
    private readonly string _interfaceName;

    public override bool IsBlittable => false;
    public override bool SupportsSetter => true;
    public override bool CacheProperty => true;
    
    public ContainerPropertyTranslator(string copyMarshallerName, string readOnlyMarshallerName, string marshallerName, string readOnlyInterfaceName, string interfaceName) : base(ContainerSupportedUsages)
    {
        _copyMarshallerName = copyMarshallerName;
        _readOnlyMarshallerName = readOnlyMarshallerName;
        _marshallerName = marshallerName;
        _readOnlyInterfaceName = readOnlyInterfaceName;
        _interfaceName = interfaceName;
    }

    public override bool CanExport(UhtProperty property)
    {
        UhtContainerBaseProperty containerProperty = (UhtContainerBaseProperty) property;
        List<UhtProperty> innerProperties = containerProperty.GetInnerProperties();
        
        foreach (UhtProperty innerProperty in innerProperties)
        {
            PropertyTranslator? translator = innerProperty.GetTranslator();
            if (translator == null || !translator.CanExport(innerProperty) || !translator.IsSupportedAsInner())
            {
                return false;
            }
        }
        
        return true;
    }

    public override string GetManagedType(UhtProperty property) => GetWrapperInterface(property);
    public override string GetMarshaller(UhtProperty property) => throw new System.NotImplementedException();
    public override string GetNullValue(UhtProperty property) => "null";
    public override string ConvertCppDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter) => throw new System.NotImplementedException();

    public override string ExportMarshallerDelegates(UhtProperty property)
    {
        UhtContainerBaseProperty containerProperty = (UhtContainerBaseProperty) property;
        List<UhtProperty> properties = containerProperty.GetInnerProperties();
        
        return string.Join(", ", properties.ConvertAll(p =>
        {
            PropertyTranslator translator = p.GetTranslator()!;
            return translator.ExportMarshallerDelegates(p);
        }));
    }

    public override void ExportPropertyGetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        ExportMarshallerCreation(property, builder, propertyManagedName);
        builder.AppendLine($"return {propertyManagedName}_Marshaller.FromNative(NativeObject + {propertyManagedName}_Offset, 0);");
    }

    public override void ExportPropertySetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        ExportMarshallerCreation(property, builder, propertyManagedName);
        builder.AppendLine($"{propertyManagedName}_Marshaller.ToNative(NativeObject + {propertyManagedName}_Offset, 0, value);");
    }

    public override void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string propertyEngineName)
    {
        base.ExportPropertyVariables(builder, property, propertyEngineName);

        if (property.IsGenericType())
        {
            return;
        }

        string wrapperType = GetWrapperType(property);
        if (property.IsOuter<UhtScriptStruct>() || property.HasAnyNativeGetterSetter())
        {
            builder.AppendLine($"static {wrapperType} {propertyEngineName}_Marshaller = null;");
        }
        else
        {
            builder.AppendLine($"{wrapperType} {propertyEngineName}_Marshaller = null;");
        }
    }

    public override void ExportParameterVariables(GeneratorStringBuilder builder, UhtFunction function, string nativeMethodName, UhtProperty property, string propertyEngineName)
    {
        base.ExportParameterVariables(builder, function, nativeMethodName, property, propertyEngineName);
        builder.AppendLine($"static IntPtr {nativeMethodName}_{propertyEngineName}_NativeProperty;");

        if (property.IsGenericType())
        {
            return;
        }
        
        if (function.FunctionFlags.HasAnyFlags(EFunctionFlags.Static))
        {
            builder.AppendLine("static ");
        }
        
        builder.Append($"{GetWrapperType(property)} {nativeMethodName}_{propertyEngineName}_Marshaller = null;");
    }

    public override void ExportParameterStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, UhtFunction function, string propertyEngineName, string functionName)
    {
        base.ExportParameterStaticConstructor(builder, property, function, propertyEngineName, functionName);
        builder.AppendLine($"{functionName}_{propertyEngineName}_NativeProperty = CallGetNativePropertyFromName({functionName}_NativeFunction, \"{propertyEngineName}\");");
    }
    
    public override void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string assignmentOrReturn,
        string sourceBuffer, string offset, bool cleanupSourceBuffer, bool reuseRefMarshallers)
    {
        string marshallerType = GetWrapperType(property);
        string marshallingDelegates = ExportMarshallerDelegates(property);
        
        GetNativePropertyFieldAndMarshaller(property, propertyName, out string marshaller, out string nativeProperty);

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
            
            builder.Append($"IntPtr {propertyName}_NativeBuffer = {sourceBuffer} + {offset};");
        }

        builder.AppendLine($"{assignmentOrReturn} {marshaller}.FromNative({propertyName}_NativeBuffer, 0);");
        
        if (cleanupSourceBuffer)
        {
            ExportCleanupMarshallingBuffer(builder, property, propertyName);
        }
    }
    
    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer, string offset, string source)
    {
        string marshallerType = GetWrapperType(property);
        string marshallingDelegates = ExportMarshallerDelegates(property);
        GetNativePropertyFieldAndMarshaller(property, propertyName, out string marshaller, out string nativeProperty);

        string marshallerCreationLine = property.IsGenericType() ? $"var {marshaller} " : $"{marshaller} ??";
        builder.AppendLine(marshallerCreationLine);

        builder.Append($"= new {marshallerType}({nativeProperty}, {marshallingDelegates});");
        
        builder.AppendLine($"IntPtr {propertyName}_NativeBuffer = {destinationBuffer} + {offset};");
        builder.AppendLine($"{marshaller}.ToNative({propertyName}_NativeBuffer, 0, {source});");
    }

    public override void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property, string paramName)
    {
        string nativeMethodName = string.Empty;
        
        if (property.Outer is UhtFunction function)
        {
            nativeMethodName = function.SourceName + "_";
        }
        
        string marshaller = $"{nativeMethodName}{paramName}_Marshaller";
        builder.AppendLine($"{marshaller}.DestructInstance({paramName}_NativeBuffer, 0);");
    }
    
    public override bool CanSupportGenericType(UhtProperty property)
    {
        UhtContainerBaseProperty containerProperty = (UhtContainerBaseProperty)property;
        List<PropertyTranslator> innerProperties = containerProperty.GetInnerPropertyTranslators();
        
        bool supportsGenericType = true;
        foreach (PropertyTranslator translator in innerProperties)
        {
            if (translator.CanSupportGenericType(containerProperty))
            {
                continue;
            }
            
            supportsGenericType = false;
            break;
        }
        
        return supportsGenericType;
    }
    
    string GetWrapperType(UhtProperty property)
    {
        bool isStructProperty = property.IsOuter<UhtScriptStruct>();
        bool isParameter = property.IsOuter<UhtFunction>();
        bool isNativeGetterSetter = property.HasAnyNativeGetterSetter();
        
        string innerManagedType = GetInnerPropertiesManagedTypes((UhtContainerBaseProperty) property);
        string containerType = isStructProperty || isParameter || isNativeGetterSetter ? _copyMarshallerName : property.PropertyFlags.HasAnyFlags(EPropertyFlags.BlueprintReadOnly) ? _readOnlyMarshallerName : _marshallerName;
        
        return $"{containerType}<{innerManagedType}>";
    }

    string GetWrapperInterface(UhtProperty property)
    {
        string innerManagedType = GetInnerPropertiesManagedTypes((UhtContainerBaseProperty) property);
        string interfaceType = property.PropertyFlags.HasAnyFlags(EPropertyFlags.BlueprintReadOnly) ? _readOnlyInterfaceName : _interfaceName;
        return $"{interfaceType}<{innerManagedType}>";
    }
    
    void ExportMarshallerCreation(UhtProperty property, GeneratorStringBuilder builder, string propertyManagedName)
    {
        string wrapperType = GetWrapperType(property);
        string marshallingDelegates = ExportMarshallerDelegates(property);
        builder.AppendLine($"{propertyManagedName}_Marshaller ??= new {wrapperType}({propertyManagedName}_NativeProperty, {marshallingDelegates});");
    }
    
    string GetInnerPropertiesManagedTypes(UhtContainerBaseProperty property)
    {
        List<UhtProperty> properties = property.GetInnerProperties();
        return string.Join(", ", properties.ConvertAll(p =>
        {
            PropertyTranslator translator = p.GetTranslator()!;
            return translator.GetManagedType(p);
        }));
    }
    
    void GetNativePropertyFieldAndMarshaller(UhtProperty property, string propertyManagedName, out string marshaller, out string nativeProperty)
    {
        nativeProperty = $"{propertyManagedName}_NativeProperty";
        marshaller = $"{propertyManagedName}_Marshaller";

        if (property.Outer is not UhtFunction)
        {
            return;
        }
        
        nativeProperty = property.PrefixWithOuterName(nativeProperty);
        marshaller = property.PrefixWithOuterName(marshaller);
    }
}