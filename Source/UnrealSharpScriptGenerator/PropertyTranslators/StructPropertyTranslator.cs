using System.Text;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class StructPropertyTranslator : SimpleTypePropertyTranslator
{
    public StructPropertyTranslator() : base(typeof(UhtStructProperty))
    {
    }

    public override string GetManagedType(UhtProperty property)
    {
       UhtStructProperty structProperty = (UhtStructProperty)property; 
       return ScriptGeneratorUtilities.GetFullManagedName(structProperty.ScriptStruct);
    }

    public override string GetMarshaller(UhtProperty property)
    {
        return $"{GetManagedType(property)}Marshaller";
    }

    public override void ExportCppDefaultParameterAsLocalVariable(StringBuilder builder, string variableName, string defaultValue,
        UhtFunction function, UhtProperty paramProperty)
    {
        ExportDefaultStructParameter(builder, variableName, defaultValue, paramProperty, this);
    }
}