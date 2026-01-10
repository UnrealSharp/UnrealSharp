using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class IntPropertyTranslator : BlittableTypePropertyTranslator
{
    public IntPropertyTranslator() : base(typeof(UhtIntProperty), "int")
    {
    }

    public override string GetManagedType(UhtProperty property)
    {
        if (property.Outer is UhtFunction function && property.IsCustomStructureType())
        {
            return function.GetCustomStructParamCount() == 1 ? "CSP" : $"CSP{property.GetPrecedingCustomStructParams()}";
        }
        
        return base.GetManagedType(property);
    }

    public override string GetMarshaller(UhtProperty property)
    {
        if (property.Outer is UhtFunction && property.IsCustomStructureType())
        {
            return $"StructMarshaller<{GetManagedType(property)}>";
        }
        
        return base.GetMarshaller(property);
    }

    public override bool CanSupportCustomStruct(UhtProperty property) => true;
}