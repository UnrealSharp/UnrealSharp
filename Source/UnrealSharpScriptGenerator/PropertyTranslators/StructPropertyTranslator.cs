using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class StructPropertyTranslator : SimpleTypePropertyTranslator
{
    public StructPropertyTranslator() : base(typeof(UhtStructProperty))
    {
    }
    
    public override bool ExportDefaultParameter => false;

    public override string GetManagedType(UhtProperty property)
    {
       UhtStructProperty structProperty = (UhtStructProperty)property; 
       return structProperty.ScriptStruct.GetFullManagedName();
    }

    public override string GetMarshaller(UhtProperty property)
    {
        return $"{GetManagedType(property)}Marshaller";
    }

    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName, string defaultValue,
        UhtFunction function, UhtProperty paramProperty)
    {
        ExportDefaultStructParameter(builder, variableName, defaultValue, paramProperty, this);
    }
}