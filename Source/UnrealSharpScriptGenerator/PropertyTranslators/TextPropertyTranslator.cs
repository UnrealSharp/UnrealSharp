using System;
using System.Text;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class TextPropertyTranslator : BlittableTypePropertyTranslator
{
    public TextPropertyTranslator() : base(typeof(UhtTextProperty), "FText")
    {
    }
    
    public override bool ExportDefaultParameter => false;
    
    public override string GetNullValue(UhtProperty property)
    {
        return "FText.None";
    }

    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName, string defaultValue,
        UhtFunction function, UhtProperty paramProperty)
    {
        builder.AppendLine($"FText {variableName} = FText.None;");
    }

    public override string GetMarshaller(UhtProperty property)
    {
        return "TextMarshaller";
    }
}