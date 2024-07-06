using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;

namespace UnrealSharpScriptGenerator.Utilities;

public static class StructUtilities
{
    public static bool IsStructBlittable(this UhtStruct structObj)
    {
        foreach (UhtType child in structObj.Children)
        {
            UhtProperty property = (UhtProperty) child;

            PropertyTranslator? propertyTranslator = PropertyTranslatorManager.GetTranslator(property);
            if (propertyTranslator != null && property.PropertyFlags.HasFlag(EPropertyFlags.BlueprintVisible) && propertyTranslator.IsBlittable)
            {
                continue;
            }
            
            return false;
        }

        return true;
    }
}