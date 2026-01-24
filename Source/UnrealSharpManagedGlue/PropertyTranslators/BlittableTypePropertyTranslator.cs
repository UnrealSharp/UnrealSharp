using System;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class BlittableTypePropertyTranslator : SimpleTypePropertyTranslator
{
    public BlittableTypePropertyTranslator(Type propertyType, string managedType) : base(propertyType, managedType, string.Empty)
    {
    }

    public override bool ExportDefaultParameter => true;
    public override string GetMarshaller(UhtProperty property) => $"BlittableMarshaller<{GetManagedType(property)}>";
    public override bool CanSupportGenericType(UhtProperty property) => false;
    
    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName, string defaultValue,
        UhtFunction function, UhtProperty paramProperty)
    {
        string defaultValueString = ConvertCppDefaultValue(defaultValue, function, paramProperty);
        builder.AppendLine($"{ManagedType} {variableName} = {defaultValueString};");
    }
}