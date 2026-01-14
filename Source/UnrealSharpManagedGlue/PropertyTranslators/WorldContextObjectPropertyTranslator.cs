using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class WorldContextObjectPropertyTranslator : ObjectPropertyTranslator
{
    public override bool ShouldBeDeclaredAsParameter => false;

    public override bool CanExport(UhtProperty property)
    {
        return base.CanExport(property) && property.IsWorldContextParameter();
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string destinationBuffer,
        string offset, string source, bool reuseRefMarshallers)
    {
        builder.AppendLine($"BlittableMarshaller<IntPtr>.ToNative({destinationBuffer} + {offset}, 0, UnrealSharp.Core.FCSManagerExporter.CallGetCurrentWorldContext());");
    }

    public override bool CanSupportGenericType(UhtProperty property) => false;
}