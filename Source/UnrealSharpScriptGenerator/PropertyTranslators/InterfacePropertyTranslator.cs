using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

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
}