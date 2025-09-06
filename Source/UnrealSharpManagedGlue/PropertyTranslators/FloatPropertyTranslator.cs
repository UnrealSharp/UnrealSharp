using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class FloatPropertyTranslator : BlittableTypePropertyTranslator
{
    public FloatPropertyTranslator() : base(typeof(UhtFloatProperty), "float", PropertyKind.Float)
    {
    }

    public override string ConvertCPPDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter)
    {
        return base.ConvertCPPDefaultValue(defaultValue, function, parameter) + "f";
    }
}