using System;
using System.Collections.Generic;
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

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer,
	    string offset, string source)
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
	    
	    if (fieldInitializerList.Length == 0)
	    {
		    return;
	    }
	    
	    List<string> fieldInitializers = new List<string>();
	    string[] parts = fieldInitializerList.Split(',');

	    foreach (string part in parts)
	    {
		    fieldInitializers.Add(part.Trim());
	    }

		string foundCSharpType = translator.GetManagedType(paramProperty);
		builder.AppendLine($"{foundCSharpType} {variableName} = new {foundCSharpType}");
		builder.OpenBrace();
		
		if (structName == "Color")
		{
			(fieldInitializers[0], fieldInitializers[2]) = (fieldInitializers[2], fieldInitializers[0]);
		}
		
		int fieldCount = fieldInitializers.Count;
		for (int i = 0; i < fieldCount; i++)
		{
			UhtProperty property = (UhtProperty) structProperty.ScriptStruct.Children[i];
			PropertyTranslator propertyTranslator = property.GetTranslator()!;
			
			string managedType = propertyTranslator.GetManagedType(property);
			bool isFloat = managedType is "float" or "double";
			
			string scriptName = property.SourceName;
			string fieldInitializer = fieldInitializers[i];

			int pos = fieldInitializer.IndexOf("=", StringComparison.Ordinal);
			if (pos < 0)
			{
				builder.AppendLine(isFloat ? $"{scriptName}={fieldInitializer}f," : $"{scriptName}={fieldInitializer},");
			}
			else
			{
				builder.AppendLine(isFloat ? $"{fieldInitializer}f," : $"{fieldInitializer},");
			}
		}
		
		builder.CloseBrace();
		builder.Append(";");
    }

    public override void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property, string paramName)
    {
	    
    }

	public override bool CanSupportGenericType(UhtProperty property) => true;
}