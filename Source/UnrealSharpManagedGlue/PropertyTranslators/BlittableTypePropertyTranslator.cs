using System;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class BlittableTypePropertyTranslator : SimpleTypePropertyTranslator
{
    public BlittableTypePropertyTranslator(Type propertyType, string managedType) : base(propertyType, managedType)
    {
    }
    public override bool ExportDefaultParameter => true;

    public override string GetMarshaller(UhtProperty property)
    {
        return $"BlittableMarshaller<{GetManagedType(property)}>";
    }
    
    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName, string defaultValue,
        UhtFunction function, UhtProperty paramProperty)
    {
        string defaultValueString = ConvertCPPDefaultValue(defaultValue, function, paramProperty);
        builder.AppendLine($"{ManagedType} {variableName} = {defaultValueString};");
    }

    public override bool CanSupportGenericType(UhtProperty property) => false;
}