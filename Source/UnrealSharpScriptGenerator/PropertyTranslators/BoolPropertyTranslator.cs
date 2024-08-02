using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class BoolPropertyTranslator : SimpleTypePropertyTranslator
{
    public BoolPropertyTranslator() : base(typeof(UhtBoolProperty), "bool")
    {
    }

    public override string GetMarshaller(UhtProperty property)
    {
        return "BoolMarshaller";
    }
}