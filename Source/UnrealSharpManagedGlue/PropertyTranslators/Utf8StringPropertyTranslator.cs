using System.Text.Json;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class Utf8StringPropertyTranslator : SimpleTypePropertyTranslator
{
    public Utf8StringPropertyTranslator() : base(typeof(UhtUtf8StrProperty), "UnrealSharp.Core.FUtf8String", "Utf8StringMarshaller")
    {
    }

    public override bool IsBlittable => false;
    public override bool ExportDefaultParameter => false;
    public override bool CanSupportGenericType(UhtProperty property) => false;

    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName,
        string defaultValue, UhtFunction function, UhtProperty paramProperty)
    {
        builder.AppendLine($"UnrealSharp.Core.FUtf8String {variableName} = new({JsonSerializer.Serialize(defaultValue)});");
    }
}
