using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class BoolPropertyTranslator : SimpleTypePropertyTranslator
{
    private const string FieldMaskPostfix = "_FieldMask";
    
    public BoolPropertyTranslator() : base(typeof(UhtBoolProperty), "bool", string.Empty)
    {
    }
    
    public bool IsBitfield(UhtProperty property)
    {
        return property.IsBitfield && !property.HasGetterSetterPair();
    }

    public override string GetMarshaller(UhtProperty property)
    {
        return IsBitfield(property) ? "BitfieldBoolMarshaller" : "BoolMarshaller";
    }

    public override void ExportPropertyStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        if (IsBitfield(property))
        {
            builder.AppendLine($"{GetFieldMaskFieldName(nativePropertyName)} = CallGetBoolPropertyFieldMaskFromName(NativeClassPtr, \"{nativePropertyName}\");");
        }
        
        base.ExportPropertyStaticConstructor(builder, property, nativePropertyName);
    }
    
    public override void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string propertyEngineName)
    {
        if (IsBitfield(property))
        {
            builder.AppendLine($"static byte {GetFieldMaskFieldName(propertyEngineName)};");
        }
    
        base.ExportPropertyVariables(builder, property, propertyEngineName);
    }
    
    public override void ExportToNative(GeneratorStringBuilder builder,
        UhtProperty property,
        string propertyName,
        string destinationBuffer,
        string offset,
        string source, bool reuseRefMarshallers)
    {
        if (IsBitfield(property))
        {
            builder.AppendLine($"{GetMarshaller(property)}.ToNative({destinationBuffer} + {offset}, {GetFieldMaskFieldName(propertyName)}, {source});");
            return;
        }
        
        base.ExportToNative(builder, property, propertyName, destinationBuffer, offset, source, false);
    }

    public override void ExportFromNative(GeneratorStringBuilder builder,
        UhtProperty property,
        string propertyName,
        string assignmentOrReturn,
        string sourceBuffer,
        string offset,
        bool cleanupSourceBuffer,
        bool reuseRefMarshallers)
    {
        if (IsBitfield(property))
        {
            builder.AppendLine($"{assignmentOrReturn} {GetMarshaller(property)}.FromNative({sourceBuffer} + {offset}, {GetFieldMaskFieldName(propertyName)});");
            return;
        }
        
        base.ExportFromNative(builder, property, propertyName, assignmentOrReturn, sourceBuffer, offset, cleanupSourceBuffer, reuseRefMarshallers);
    }
    
    private string GetFieldMaskFieldName(string nativePropertyName)
    {
        return $"{nativePropertyName}{FieldMaskPostfix}";
    }
}