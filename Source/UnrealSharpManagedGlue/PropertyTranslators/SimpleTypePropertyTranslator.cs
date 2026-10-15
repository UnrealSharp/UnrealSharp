using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

    protected void ExportDefaultStructParameter(GeneratorStringBuilder builder, string variableName, string cppDefaultValue,
        UhtProperty paramProperty, PropertyTranslator translator)
    {
	    UhtStructProperty structProperty = (UhtStructProperty)paramProperty;
	    string foundCSharpType = translator.GetManagedType(paramProperty);

	    // FSoftClassPath uses a top-level asset path instead of a field initializer list.
	    if (structProperty.ScriptStruct.SourceName == "FSoftClassPath" && cppDefaultValue.StartsWith("/", StringComparison.Ordinal))
	    {
		    int separator = cppDefaultValue.LastIndexOf('.');
		    string packageName = JsonSerializer.Serialize(cppDefaultValue.Substring(0, separator));
		    string assetName = JsonSerializer.Serialize(cppDefaultValue.Substring(separator + 1));
		    builder.AppendLine($"{foundCSharpType} {variableName} = new {foundCSharpType}(" +
			    $"new global::UnrealSharp.CoreUObject.FTopLevelAssetPath(new FName({packageName}), new FName({assetName})));");
		    return;
	    }

	    UnrealDefaultValueParser.Value value = UnrealDefaultValueParser.Parse(cppDefaultValue);
	    if (value.Fields!.Count == 0)
	    {
		    return;
	    }

		builder.AppendLine($"{foundCSharpType} {variableName} = new {foundCSharpType}");
		ExportDefaultStructFields(builder, structProperty, value);
		builder.Append(";");
    }

    private static void ExportDefaultStructFields(GeneratorStringBuilder builder, UhtStructProperty structProperty,
	    UnrealDefaultValueParser.Value value)
    {
	    string structName = structProperty.ScriptStruct.GetStructName();
	    List<UnrealDefaultValueParser.Field> fieldInitializers = value.Fields?.ToList()
		    ?? throw new FormatException($"Expected struct fields in '{value.Text}'.");

		builder.OpenBrace();

		if (structName == "Color")
		{
			(fieldInitializers[0], fieldInitializers[2]) = (fieldInitializers[2], fieldInitializers[0]);
		}

		int fieldCount = fieldInitializers.Count;
		for (int i = 0; i < fieldCount; i++)
		{
			UnrealDefaultValueParser.Field initializer = fieldInitializers[i];
			UhtProperty property = initializer.Name == null
				? (UhtProperty) structProperty.ScriptStruct.Children[i]
				: structProperty.ScriptStruct.Properties.First(x =>
					string.Equals(x.SourceName, initializer.Name, StringComparison.OrdinalIgnoreCase));
			PropertyTranslator propertyTranslator = property.GetTranslator()!;

			string managedType = propertyTranslator.GetManagedType(property);
			bool isFloat = managedType is "float" or "double";

			string scriptName = property.SourceName;
			if (structName == "FTransform")
			{
				scriptName = scriptName switch
				{
					"Translation" => "Location",
					"Scale3D" => "Scale",
					_ => scriptName,
				};
			}

			if (property is UhtStructProperty nestedStruct)
			{
				builder.AppendLine($"{scriptName} = new {managedType}");
				ExportDefaultStructFields(builder, nestedStruct, initializer.Value);
				builder.Append(",");
			}
			else
			{
				string fieldInitializer = initializer.Value.Text;
				builder.AppendLine(isFloat ? $"{scriptName}={fieldInitializer}f," : $"{scriptName}={fieldInitializer},");
			}
		}

		builder.CloseBrace();
    }

    public override void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property, string paramName)
    {
	    
    }

	public override bool CanSupportGenericType(UhtProperty property) => true;
}
