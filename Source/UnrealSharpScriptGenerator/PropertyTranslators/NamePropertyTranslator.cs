using System;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class NamePropertyTranslator : BlittableTypePropertyTranslator
{
    public NamePropertyTranslator() : base(typeof(UhtNameProperty), "Name")
    {
    }
    
    public override bool ExportDefaultParameter => false;

    public override string GetNullValue(UhtProperty property)
    {
        return "default(Name)";
    }

    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName, string defaultValue,
        UhtFunction function, UhtProperty paramProperty)
    {
        if (defaultValue == "None")
        {
            builder.AppendLine($"Name {variableName} = Name.None;");
        }
        else
        {
            builder.AppendLine($"Name {variableName} = Name(\"{defaultValue}\");");
        }
    }
}