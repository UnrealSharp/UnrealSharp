using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class TextPropertyTranslator : BlittableTypePropertyTranslator
{
    const string TextNullValue = "FText.None";
    
    public TextPropertyTranslator() : base(typeof(UhtTextProperty), "FText")
    {
    }
    
    public override bool ExportDefaultParameter => false;

    public override string GetNullValue(UhtProperty property) => TextNullValue;
    public override string GetMarshaller(UhtProperty property) => "TextMarshaller";
    public override bool CanSupportGenericType(UhtProperty property) => false;

    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName, string defaultValue, UhtFunction function, UhtProperty paramProperty)
    {
        builder.AppendLine($"FText {variableName} = {TextNullValue};");
    }
}