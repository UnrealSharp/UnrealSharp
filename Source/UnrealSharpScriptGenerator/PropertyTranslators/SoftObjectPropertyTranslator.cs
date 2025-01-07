using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class SoftObjectPropertyTranslator : SimpleTypePropertyTranslator
{
    public SoftObjectPropertyTranslator() : base(typeof(UhtSoftObjectProperty))
    {
    }
    
    public override bool CanExport(UhtProperty property)
    {
        return property is UhtSoftObjectProperty;
    }

    public override string GetManagedType(UhtProperty property)
    {
        UhtSoftObjectProperty softObjectProperty = (UhtSoftObjectProperty)property;
        string fullName = softObjectProperty.Class.GetFullManagedName();
        return $"TSoftObjectPtr<{fullName}>";
    }

    public override string GetMarshaller(UhtProperty property)
    {
        UhtSoftObjectProperty softClassProperty = (UhtSoftObjectProperty) property;
        string fullName = softClassProperty.Class.GetFullManagedName();
        return $"SoftObjectMarshaller<{fullName}>";
    }

    public override bool CanSupportGenericType(UhtProperty property) => false;
}