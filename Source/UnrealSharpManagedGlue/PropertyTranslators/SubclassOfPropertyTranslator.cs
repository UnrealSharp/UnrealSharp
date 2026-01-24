using EpicGames.UHT.Types;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class SubclassOfPropertyTranslator : ObjectContainerPropertyTranslator
{
	public SubclassOfPropertyTranslator() : base(typeof(UhtClassProperty), "TSubclassOf", "SubclassOfMarshaller")
	{
	}

	protected override UhtClass GetMetaClass(UhtObjectPropertyBase property)
	{
		UhtClassProperty classProperty = (UhtClassProperty)property;
		return classProperty.MetaClass!;
	}
}