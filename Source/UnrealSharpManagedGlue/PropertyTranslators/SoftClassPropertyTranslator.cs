using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class SoftClassPropertyTranslator : SimpleTypePropertyTranslator
{
    public SoftClassPropertyTranslator() : base(typeof(UhtSoftClassProperty))
    {
        
    }
    
    public override string GetManagedType(UhtProperty property)
    {
        UhtSoftClassProperty softClassProperty = (UhtSoftClassProperty)property;
        string fullName = property.IsGenericType()
             ? "DOT" : softClassProperty.MetaClass.GetFullManagedName();

        return $"TSoftClassPtr<{fullName}>";
    }

    public override string GetMarshaller(UhtProperty property)
    {
        UhtSoftClassProperty softClassProperty = (UhtSoftClassProperty) property;
        string fullName = property.IsGenericType()
             ? "DOT" : softClassProperty.MetaClass.GetFullManagedName();

        return $"SoftClassMarshaller<{fullName}>";
    }

    public override bool CanExport(UhtProperty property)
    {
        return property is UhtSoftClassProperty;
    }
}
