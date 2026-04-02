using EpicGames.UHT.Types;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class SoftClassPropertyTranslator : ObjectContainerPropertyTranslator
{
    public SoftClassPropertyTranslator() : base(typeof(UhtSoftClassProperty), "TSoftClassPtr", "SoftClassMarshaller")
    {
        
    }

    protected override UhtClass GetMetaClass(UhtObjectPropertyBase property)
    {
        UhtSoftClassProperty softClassProperty = (UhtSoftClassProperty)property;
        return softClassProperty.MetaClass!;
    }
}
