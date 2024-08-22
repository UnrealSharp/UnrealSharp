using System;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class EnumPropertyHandler : BlittableTypePropertyTranslator
{
    public EnumPropertyHandler() : base(typeof(UhtByteProperty), "")
    {
    }

    public override bool CanExport(UhtProperty property)
    {
        return property is UhtEnumProperty or UhtByteProperty && GetEnum(property) != null;
    }

    public override string ConvertCPPDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter)
    {
        UhtEnum enumObj = GetEnum(parameter)!;
        int index = enumObj.GetIndexByName(defaultValue);
        string valueName = ScriptGeneratorUtilities.GetCleanEnumValueName(enumObj, enumObj.EnumValues[index]);
        return $"{GetManagedType(parameter)}.{valueName}";
    }
    
    public override string GetManagedType(UhtProperty property)
    {
        UhtEnum enumObj = GetEnum(property)!;
        return enumObj.GetFullManagedName();
    }
    
    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName, string defaultValue,
        UhtFunction function, UhtProperty paramProperty)
    {
        UhtEnum enumObj = GetEnum(paramProperty)!;
        
        if (defaultValue.Contains("::"))
        {
            defaultValue = defaultValue.Substring(defaultValue.LastIndexOf("::") + 2);
        }
        
        string fullEnumName = GetManagedType(paramProperty);
        builder.AppendLine($"{fullEnumName} {variableName} = {fullEnumName}.{defaultValue};");
    }

    private static UhtEnum? GetEnum(UhtProperty property)
    {
        return property switch
        {
            UhtEnumProperty enumProperty => enumProperty.Enum,
            UhtByteProperty byteProperty => byteProperty.Enum,
            _ => throw new InvalidOperationException("Property is not an enum or byte property")
        };
    }
}