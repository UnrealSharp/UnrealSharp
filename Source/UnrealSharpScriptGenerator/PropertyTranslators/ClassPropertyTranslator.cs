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
        string fullName = property.IsGenericType() ? "DOT" 
            : classProperty.MetaClass!.GetFullManagedName();

        return $"TSubclassOf<{fullName}>";
    }

    public override string GetMarshaller(UhtProperty property)
    {
        UhtClassProperty classProperty = (UhtClassProperty)property;
        string fullName = property.IsGenericType() ? "DOT"
            : classProperty.MetaClass!.GetFullManagedName();

        return $"SubclassOfMarshaller<{fullName}>";
    }

    /*
    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer, string offset, string source)
    {
        if (property.IsGenericType())
        {
            builder.AppendLine($"{GetMarshaller(property)}.ToNative(IntPtr.Add({destinationBuffer}, {offset}), 0, typeof(DOT));");
        }
        else
        {
            base.ExportToNative(builder, property, propertyName, destinationBuffer, offset, source);
        }
    }*/
}