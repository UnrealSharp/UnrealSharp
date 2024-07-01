using System;
using System.Text;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class TextPropertyTranslator : BlittableTypePropertyTranslator
{
    public TextPropertyTranslator() : base(typeof(UhtTextProperty), "Text")
    {
    }

    public override string GetNullValue(UhtProperty property)
    {
        return "Text.None";
    }

    public override void ExportCppDefaultParameterAsLocalVariable(StringBuilder builder, string variableName, string defaultValue,
        UhtFunction function, UhtProperty paramProperty)
    {
        builder.AppendLine($"Text {variableName} = Text.None;");
    }

    public override string GetMarshaller(UhtProperty property)
    {
        return "TextMarshaller";
    }
}