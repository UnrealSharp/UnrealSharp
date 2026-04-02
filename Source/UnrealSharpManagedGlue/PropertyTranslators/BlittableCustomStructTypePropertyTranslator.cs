using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class BlittableCustomStructTypePropertyTranslator : BlittableTypePropertyTranslator
{
    public override bool ExportDefaultParameter => false;
    private readonly string _nativeName;
    
    public BlittableCustomStructTypePropertyTranslator(string nativeName, string managedType) : base(typeof(UhtStructProperty), managedType)
    {
        _nativeName = nativeName;
    }

    public override bool CanExport(UhtProperty property)
    {
        UhtStructProperty structProperty = (UhtStructProperty) property;
        return structProperty.ScriptStruct.SourceName == _nativeName;
    }

    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName, string defaultValue,
        UhtFunction function, UhtProperty paramProperty)
    {
        ExportDefaultStructParameter(builder, variableName, defaultValue, paramProperty, this);
    }
}