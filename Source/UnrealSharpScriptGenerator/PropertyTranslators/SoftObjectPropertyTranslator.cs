using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class SoftObjectPropertyTranslator : BlittableStructPropertyTranslator
{
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

    public override void GetReferences(UhtProperty property, List<UhtType> references)
    {
        base.GetReferences(property, references);
        UhtSoftObjectProperty softObjectProperty = (UhtSoftObjectProperty)property;
        references.Add(softObjectProperty.Class);
    }
}