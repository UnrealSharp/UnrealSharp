using System;
using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class BlittableStructPropertyTranslator : BlittableTypePropertyTranslator
{
    public BlittableStructPropertyTranslator() : base(typeof(UhtStructProperty), "")
    {
    }

    bool IsStructBlittable(UhtStruct structObj)
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

    public override bool CanExport(UhtProperty property)
    {
        UhtStructProperty structProperty = (UhtStructProperty) property;
        return IsStructBlittable(structProperty.ScriptStruct);
    }

    public override string GetManagedType(UhtProperty property)
    {
        UhtStructProperty structProperty = (UhtStructProperty) property;
        return ScriptGeneratorUtilities.GetFullManagedName(structProperty.ScriptStruct);
    }

    public override void ExportCppDefaultParameterAsLocalVariable(StringBuilder builder, string variableName, string defaultValue,
        UhtFunction function, UhtProperty paramProperty)
    {
        ExportDefaultStructParameter(builder, variableName, defaultValue, paramProperty, this);
    }
}