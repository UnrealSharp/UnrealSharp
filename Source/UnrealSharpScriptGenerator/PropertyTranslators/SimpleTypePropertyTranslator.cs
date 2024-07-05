﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class SimpleTypePropertyTranslator : PropertyTranslators.PropertyTranslator
{
    public override bool IsBlittable => true;
    
    private readonly Type _propertyType;
    private readonly string? _managedType;

    public SimpleTypePropertyTranslator(Type propertyType, string? managedType = "") : base(EPropertyUsageFlags.Any)
    {
        _propertyType = propertyType;
        _managedType = managedType;
    }

    public override string ConvertCPPDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter)
    {
        if (defaultValue == "None")
        {
            return GetNullValue(parameter);
        }
        
        return defaultValue;
    }
    
    public override string GetNullValue(UhtProperty property)
    {
        string managedType = GetManagedType(property);
        return $"default({managedType})";
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string assignmentOrReturn, string sourceBuffer, string offset, bool bCleanupSourceBuffer,
        bool reuseRefMarshallers)
    {
        builder.AppendLine($"{assignmentOrReturn} {GetMarshaller(property)}.FromNative(IntPtr.Add({sourceBuffer}, {offset}), 0);");
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer,
	    string offset, string source)
    {
	    builder.AppendLine($"{GetMarshaller(property)}.ToNative(IntPtr.Add({destinationBuffer}, {offset}), 0, {source});");
    }

    public override string GetMarshaller(UhtProperty property)
    {
	    throw new NotImplementedException();
    }
    
    public virtual string GetPropertyName(UhtProperty property)
	{
		return property.SourceName;
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
        return _managedType;
    }

    protected void ExportDefaultStructParameter(GeneratorStringBuilder builder, string variableName, string cppDefaultValue,
        UhtProperty paramProperty, PropertyTranslator translator)
    {
	    UhtStructProperty structProperty = (UhtStructProperty)paramProperty;
	    string structName = structProperty.ScriptStruct.SourceName;

	    string fieldInitializerList;
	    if (cppDefaultValue.StartsWith("(") && cppDefaultValue.EndsWith(")"))
	    {
		    fieldInitializerList = cppDefaultValue.Substring(1, cppDefaultValue.Length - 2);
	    }
	    else
	    {
		    fieldInitializerList = cppDefaultValue;
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
		builder.Indent();

		bool isFloat = true;
		if (structName == "Color")
		{
			isFloat = false;
			(fieldInitializers[0], fieldInitializers[2]) = (fieldInitializers[2], fieldInitializers[0]);
		}
		
		int fieldCount = fieldInitializers.Count;
		for (int i = 0; i < fieldCount; i++)
		{
			UhtType prop = structProperty.ScriptStruct.Children[i];
			string fieldInitializer = fieldInitializers[i];

			int pos = fieldInitializer.IndexOf("=", StringComparison.Ordinal);
			if (pos < 0)
			{
				builder.AppendLine(isFloat ? $"{prop.SourceName}={prop.SourceName}f," : $"{prop.SourceName}={fieldInitializer},");
			}
			else
			{
				builder.AppendLine(isFloat ? $"{fieldInitializer}f," : $"{fieldInitializer},");
			}
		}
		
		builder.UnIndent();
		builder.CloseBraceWithSemicolon();
    }

    public override void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property, string paramName)
    {
	    // No cleanup needed
    }
}