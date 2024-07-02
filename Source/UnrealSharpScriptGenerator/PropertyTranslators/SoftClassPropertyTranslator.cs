using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class SoftClassPropertyTranslator : SoftObjectPropertyTranslator
{
    public override string GetManagedType(UhtProperty property)
    {
        UhtSoftClassProperty softClassProperty = (UhtSoftClassProperty)property;
        string fullName = ScriptGeneratorUtilities.GetFullManagedName(softClassProperty.Class);
        return $"SoftClass<{fullName}>";
    }
    
    public override bool CanExport(UhtProperty property)
    {
        return property is UhtSoftClassProperty;
    }
}