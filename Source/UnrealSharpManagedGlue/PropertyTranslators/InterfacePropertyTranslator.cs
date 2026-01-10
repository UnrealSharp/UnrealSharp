using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class InterfacePropertyTranslator : SimpleTypePropertyTranslator
{
    public InterfacePropertyTranslator() : base(typeof(UhtInterfaceProperty))
    {
    }

    public override string GetManagedType(UhtProperty property)
    {
        UhtInterfaceProperty interfaceProperty = (UhtInterfaceProperty)property;
        return interfaceProperty.InterfaceClass.GetFullManagedName();
    }

    public override string GetMarshaller(UhtProperty property)
    {
        return $"{GetManagedType(property)}Marshaller";
    }

    public override bool CanSupportGenericType(UhtProperty property) => true;
}