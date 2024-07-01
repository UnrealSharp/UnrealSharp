using EpicGames.UHT.Types;

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
        string fullName = ScriptGeneratorUtilities.GetFullManagedName(softObjectProperty.Class);
        return $"SoftObject<{fullName}>";
    }
}