using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class StructPropertyTranslator : SimpleTypePropertyTranslator
{
    public StructPropertyTranslator() : base(typeof(UhtStructProperty))
    {
    }
    
    public override bool ExportDefaultParameter => false;
    public override bool IsBlittable => false;

    public override string GetManagedType(UhtProperty property)
    {
       UhtStructProperty structProperty = (UhtStructProperty)property; 
       return structProperty.ScriptStruct.GetFullManagedName();
    }

    public override string GetMarshaller(UhtProperty property)
    {
        return $"StructMarshaller<{GetManagedType(property)}>";
    }

    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName, string defaultValue,
        UhtFunction function, UhtProperty paramProperty)
    {
        ExportDefaultStructParameter(builder, variableName, defaultValue, paramProperty, this);
    }

    public override bool CanSupportGenericType(UhtProperty property) => false;
}