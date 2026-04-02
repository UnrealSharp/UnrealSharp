using EpicGames.UHT.Types;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class SoftObjectPropertyTranslator : ObjectContainerPropertyTranslator
{
    public SoftObjectPropertyTranslator() : base(typeof(UhtSoftObjectProperty), "TSoftObjectPtr", "SoftObjectMarshaller")
    {
    }

    protected override UhtClass GetMetaClass(UhtObjectPropertyBase property)
    {
        UhtSoftObjectProperty softObjectProperty = (UhtSoftObjectProperty) property;
        return softObjectProperty.Class;
    }
}