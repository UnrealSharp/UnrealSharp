using System;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class SimpleTypePropertyTranslator : PropertyTranslator
{
    public override bool IsBlittable => true;
	
    protected readonly string ManagedType;
    protected readonly string Marshaller;
    
    private readonly Type _propertyType;
    
    protected SimpleTypePropertyTranslator(Type propertyType, string managedType = "", string marshaller = "") : base(EPropertyUsageFlags.Any)
    {
        _propertyType = propertyType;
        ManagedType = managedType;
        Marshaller = marshaller;
    }

    public override string ConvertCppDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter)
    {
	    if (parameter is UhtBoolProperty && StructDefaultValue.TryScalar(parameter, defaultValue, out string boolean))
	        return boolean;
	    return defaultValue == "None" ? GetNullValue(parameter) : defaultValue;
    }
    
    public override string GetNullValue(UhtProperty property)
    {
        string managedType = GetManagedType(property);
        return $"default({managedType})";
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
	    string assignmentOrReturn, string sourceBuffer, string offset, bool cleanupSourceBuffer,
	    bool reuseRefMarshallers)
    {
        builder.AppendLine($"{assignmentOrReturn} {GetMarshaller(property)}.FromNative({sourceBuffer} + {offset}, 0);");
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
	    string destinationBuffer,
	    string offset, string source, bool reuseRefMarshallers)
    {
	    builder.AppendLine($"{GetMarshaller(property)}.ToNative({destinationBuffer} + {offset}, 0, {source});");
    }

    public override string GetMarshaller(UhtProperty property)
    {
	    return Marshaller;
    }

    public override string ExportMarshallerDelegates(UhtProperty property)
    {
        string marshaller = GetMarshaller(property);
        return $"{marshaller}.ToNative, {marshaller}.FromNative";
    }

    public override bool CanExport(UhtProperty property)
    {
        return property.GetType() == _propertyType || property.GetType().IsSubclassOf(_propertyType);
    }

    public override string GetManagedType(UhtProperty property)
    {
        return property.IsGenericType() ? "DOT" : ManagedType;
    }

    public override bool CanExportDefaultValue(UhtFunction function, UhtProperty property, string value)
    {
        if (property is UhtStructProperty structure)
            return StructDefaultValue.TryTranslate(structure, value, out _);
        if (property is UhtNumericProperty or UhtBoolProperty or UhtEnumProperty)
            return StructDefaultValue.TryScalar(property, value, out _);
        return true;
    }

    protected void ExportDefaultStructParameter(GeneratorStringBuilder builder, string variableName, string cppDefaultValue,
        UhtProperty paramProperty, PropertyTranslator translator)
    {
        if (!StructDefaultValue.TryTranslate((UhtStructProperty)paramProperty, cppDefaultValue, out string expression))
            throw new InvalidOperationException("Unsupported struct default passed the default-value preflight.");
        builder.AppendLine($"{translator.GetManagedType(paramProperty)} {variableName} = {expression};");
    }

    public override void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property, string paramName)
    {
	    
    }

	public override bool CanSupportGenericType(UhtProperty property) => true;
}
