using EpicGames.UHT.Types;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class FloatPropertyTranslator : BlittableTypePropertyTranslator
{
    public FloatPropertyTranslator() : base(typeof(UhtFloatProperty), "float")
    {
    }

    public override string ConvertCppDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter)
    {
        return base.ConvertCppDefaultValue(defaultValue, function, parameter) + "f";
    }
}