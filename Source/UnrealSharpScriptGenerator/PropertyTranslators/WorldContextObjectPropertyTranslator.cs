using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class WorldContextObjectPropertyTranslator : ObjectPropertyTranslator
{
    public override bool ShouldBeDeclaredAsParameter => false;

    public override bool CanExport(UhtProperty property)
    {
        return base.CanExport(property) && property.IsWorldContextParameter();
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer,
        string offset, string source)
    {
        builder.AppendLine($"BlittableMarshaller<IntPtr>.ToNative(IntPtr.Add({destinationBuffer}, {offset}), 0, FCSManagerExporter.CallGetCurrentWorldContext());");
    }

    public override bool CanSupportGenericType(UhtProperty property) => false;
}