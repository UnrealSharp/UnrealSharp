using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;

namespace UnrealSharpScriptGenerator.Utilities;

public static class StructUtilities
{
    public static bool IsStructBlittable(this UhtStruct structObj)
    {
        int blittableCount = 0;
        foreach (UhtType child in structObj.Children)
        {
            UhtProperty property = (UhtProperty) child;

            PropertyTranslator? propertyTranslator = PropertyTranslatorManager.GetTranslator(property);
            
            if (propertyTranslator is not { IsBlittable: true })
            {
                return false;
            }
            
            blittableCount++;
        }

        return blittableCount == structObj.Children.Count && blittableCount > 0;
    }
}