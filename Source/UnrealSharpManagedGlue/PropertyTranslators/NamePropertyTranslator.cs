using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class NamePropertyTranslator : BlittableTypePropertyTranslator
{
    public NamePropertyTranslator() : base(typeof(UhtNameProperty), "FName")
    {
    }
    
    public override bool ExportDefaultParameter => false;

    public override string GetNullValue(UhtProperty property)
    {
        return "default(FName)";
    }

    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName, string defaultValue,
        UhtFunction function, UhtProperty paramProperty)
    {
        if (defaultValue == "None")
        {
            builder.AppendLine($"FName {variableName} = FName.None;");
        }
        else
        {
            builder.AppendLine($"FName {variableName} = new FName(\"{defaultValue}\");");
        }
    }
}