using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class WorldContextObjectPropertyTranslator : ObjectPropertyTranslator
{
    public override bool ShouldBeDeclaredAsParameter => false;

    public override bool CanExport(UhtProperty property)
    {
        if (!base.CanExport(property))
        {
            return false;
        }

        if (property.Outer is not UhtFunction function)
        {
            return false;
        }

        if (property is not UhtObjectProperty objectProperty || objectProperty.Class != Program.Factory.Session.UObject)
        {
            return false;
        }

        string sourceName = property.SourceName;
        return function.GetMetadata("WorldContext") == sourceName || sourceName is "WorldContextObject" or "WorldContext" or "ContextObject";
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer,
        string offset, string source)
    {
        builder.AppendLine($"BlittableMarshaller<IntPtr>.ToNative(IntPtr.Add({destinationBuffer}, {offset}), 0, FCSManagerExporter.CallGetCurrentWorldContext());");
    }
}