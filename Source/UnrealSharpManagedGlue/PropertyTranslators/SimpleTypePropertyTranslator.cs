using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
	    string structName = structProperty.ScriptStruct.GetStructName();

	    string fieldInitializerList;
	    if (cppDefaultValue.StartsWith("(") && cppDefaultValue.EndsWith(")"))
	    {
		    fieldInitializerList = cppDefaultValue.Substring(1, cppDefaultValue.Length - 2);
	    }
	    else
	    {
		    fieldInitializerList = cppDefaultValue;
	    }
	    
		string foundCSharpType = translator.GetManagedType(paramProperty);
		List<string> fieldInitializers = fieldInitializerList.Split(',').Select(part => part.Trim()).ToList();
		bool isNumericTuple = fieldInitializers.All(part =>
			double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && double.IsFinite(number));
		if (!isNumericTuple)
		{
			// CPP_Default metadata is Unreal import text, which may contain nested structs,
			// named fields, quoted commas or custom formats such as FSoftClassPath. Let the
			// native property parse it instead of treating every comma as a reflected field.
			UhtFunction function = (UhtFunction)paramProperty.Outer!;
			UhtClass owner = (UhtClass)function.Outer!;
			string escapedDefault = cppDefaultValue.Replace("\"", "\"\"");
			builder.AppendLine($"{foundCSharpType} {variableName} = global::UnrealSharp.StructDefaultValue.Parse<{foundCSharpType}>(typeof({owner.GetFullManagedName()}), \"{owner.SourceName}\", \"{function.SourceName}\", \"{paramProperty.SourceName}\", @\"{escapedDefault}\", {translator.GetMarshaller(paramProperty)}.FromNative);");
			return;
		}

		// UHT emits positional numeric defaults for its built-in math types. Keep their
		// existing field ordering (including FColor's BGRA layout) without a native call.
		UhtProperty[] fields = structProperty.ScriptStruct.Properties.ToArray();
		if (fieldInitializers.Count > fields.Length || (structName == "Color" && fieldInitializers.Count < 3))
		{
			throw new InvalidOperationException($"Invalid positional default '{cppDefaultValue}' for {structProperty.ScriptStruct.SourceName} parameter {paramProperty.SourceName}.");
		}

		builder.AppendLine($"{foundCSharpType} {variableName} = new {foundCSharpType}");
		builder.OpenBrace();
		
		if (structName == "Color")
		{
			(fieldInitializers[0], fieldInitializers[2]) = (fieldInitializers[2], fieldInitializers[0]);
		}
		
		int fieldCount = fieldInitializers.Count;
		for (int i = 0; i < fieldCount; i++)
		{
			UhtProperty property = fields[i];
			PropertyTranslator propertyTranslator = property.GetTranslator()!;
			
			string managedType = propertyTranslator.GetManagedType(property);
			bool isFloat = managedType == "float";
			
			// Built-in numeric structs use handwritten fields (e.g. FRotator.Pitch),
			// which retain native source names instead of Blueprint DisplayName aliases.
			string scriptName = property.SourceName;
			string fieldInitializer = fieldInitializers[i];
			builder.AppendLine(isFloat ? $"{scriptName}={fieldInitializer}f," : $"{scriptName}={fieldInitializer},");
		}
		
		builder.CloseBrace();
		builder.Append(";");
    }

    public override void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property, string paramName)
    {
	    
    }

	public override bool CanSupportGenericType(UhtProperty property) => true;
}