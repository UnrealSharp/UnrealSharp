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
    
    public override bool ExportDefaultParameter => false;

    public override bool CanExport(UhtProperty property)
    {
        UhtStructProperty structProperty = (UhtStructProperty) property;
        return structProperty.ScriptStruct.IsStructBlittable();
    }

    public override string GetManagedType(UhtProperty property)
    {
        UhtStructProperty structProperty = (UhtStructProperty) property;
        return structProperty.ScriptStruct.GetFullManagedName();
    }

    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName, string defaultValue,
        UhtFunction function, UhtProperty paramProperty)
    {
        ExportDefaultStructParameter(builder, variableName, defaultValue, paramProperty, this);
    }
}