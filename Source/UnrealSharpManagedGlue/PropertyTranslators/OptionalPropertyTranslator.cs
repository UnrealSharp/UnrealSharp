using System.Linq;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class OptionalPropertyTranslator : ContainerPropertyTranslator
{
    public OptionalPropertyTranslator() : base("OptionalMarshaller", "OptionalMarshaller", "OptionalMarshaller",
        "UnrealSharp.TOptional", "UnrealSharp.TOptional")
    {
    }

    public override bool CanExportDefaultValue(UhtFunction function, UhtProperty property, string value)
    {
        UhtProperty inner = ((UhtOptionalProperty)property).ValueProperty;
        return inner.GetTranslator()!.CanExportDefaultValue(function, inner, value);
    }

    public override bool ExportDefaultParameter => false;
    public override string GetNullValue(UhtProperty property) => "default";

    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName,
        string defaultValue, UhtFunction function, UhtProperty paramProperty)
    {
        UhtProperty valueProperty = ((UhtOptionalProperty)paramProperty).ValueProperty;
        PropertyTranslator valueTranslator = valueProperty.GetTranslator()!;
        string valueExpression;

        if (valueProperty is UhtStructProperty && defaultValue == "()")
        {
            valueExpression = $"new {valueTranslator.GetManagedType(valueProperty)}()";
        }
        else if (valueTranslator.ExportDefaultParameter)
        {
            valueExpression = valueTranslator.ConvertCppDefaultValue(defaultValue, function, valueProperty);
        }
        else
        {
            valueExpression = variableName + "Value";
            while (function.Properties.Any(property => property.GetParameterName() == valueExpression))
            {
                valueExpression += "Value";
            }
            valueTranslator.ExportCppDefaultParameterAsLocalVariable(builder, valueExpression, defaultValue, function, valueProperty);
        }

        string optionalType = GetManagedType(paramProperty);
        builder.AppendLine($"{optionalType} {variableName} = {optionalType}.Some({valueExpression});");
    }
}
