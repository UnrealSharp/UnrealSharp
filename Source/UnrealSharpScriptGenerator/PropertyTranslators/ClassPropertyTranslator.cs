using System;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class ClassPropertyTranslator : SimpleTypePropertyTranslator
{
    public ClassPropertyTranslator() : base(typeof(UhtClassProperty))
    {
    }

    public override string GetManagedType(UhtProperty property)
    {
        UhtClassProperty classProperty = (UhtClassProperty)property;
        string fullName = classProperty.MetaClass!.GetFullManagedName();

        if (property.HasMetaData("GenericType"))
        {
            fullName = property.GetMetaData("GenericType");
        }

        return $"TSubclassOf<{fullName}>";
    }

    public override string GetMarshaller(UhtProperty property)
    {
        UhtClassProperty classProperty = (UhtClassProperty)property;
        string fullName = classProperty.MetaClass!.GetFullManagedName();

        if (property.HasMetaData("GenericType"))
        {
            fullName = property.GetMetaData("GenericType");
        }

        return $"SubclassOfMarshaller<{fullName}>";
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer, string offset, string source)
    {
        if (property.HasMetaData("GenericType"))
        {
            var genericParam = property.GetMetaData("GenericType");

            builder.AppendLine($"{GetMarshaller(property)}.ToNative(IntPtr.Add({destinationBuffer}, {offset}), 0, typeof({genericParam}));");
        }
        else
        {
            base.ExportToNative(builder, property, propertyName, destinationBuffer, offset, source);
        }
    }
}