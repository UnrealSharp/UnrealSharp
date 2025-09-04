using System.Linq;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class IntPropertyTranslator : BlittableTypePropertyTranslator
{
    public IntPropertyTranslator() : base(typeof(UhtIntProperty), "int", PropertyKind.Int)
    {
    }

    public override string GetManagedType(UhtProperty property)
    {
        if (property.Outer is UhtFunction function && property.IsCustomStructureType())
        {
            if (function.GetCustomStructParamCount() == 1) return "CSP";
            return $"CSP{property.GetPrecedingCustomStructParams()}";
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