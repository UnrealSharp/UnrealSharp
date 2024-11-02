using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class BoolPropertyTranslator : SimpleTypePropertyTranslator
{
    private const string OffSetPostfix = "_Offset";
    private const string FieldMaskPostfix = "_FieldMask";
    
    public BoolPropertyTranslator() : base(typeof(UhtBoolProperty), "bool")
    {
    }

    public override string GetMarshaller(UhtProperty property)
    {
        return property.IsBitfield ? "BitfieldBoolMarshaller" : "BoolMarshaller";
    }

    public override void ExportPropertyStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        if (property.IsBitfield)
        {
            builder.AppendLine($"{GetOffsetFieldName(nativePropertyName)} = {ExporterCallbacks.FPropertyCallbacks}.CallGetPropertyOffsetFromName(NativeClassPtr, \"{nativePropertyName}\");");
            builder.AppendLine($"{GetFieldMaskFieldName(nativePropertyName)} = {ExporterCallbacks.FPropertyCallbacks}.CallGetBoolPropertyFieldMaskFromName(NativeClassPtr, \"{nativePropertyName}\");");
            return;
        }
        
        base.ExportPropertyStaticConstructor(builder, property, nativePropertyName);
    }
    
    public override void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string propertyEngineName)
    {
        if (property.IsBitfield)
        {
            builder.AppendLine($"static int {GetOffsetFieldName(propertyEngineName)};");
            builder.AppendLine($"static byte {GetFieldMaskFieldName(propertyEngineName)};");
            return;
        }
    
        base.ExportPropertyVariables(builder, property, propertyEngineName);
    }
    
    public override void ExportToNative(
        GeneratorStringBuilder builder, 
        UhtProperty property, 
        string propertyName, 
        string destinationBuffer,
        string offset, 
        string source)
    {
        if (property.IsBitfield)
        {
            builder.AppendLine($"{GetMarshaller(property)}.ToNative(IntPtr.Add({destinationBuffer}, {offset}), {GetFieldMaskFieldName(propertyName)}, {source});");
            return;
        }
        
        base.ExportToNative(builder, property, propertyName, destinationBuffer, offset, source);
    }

    public override void ExportFromNative(
        GeneratorStringBuilder builder,
        UhtProperty property, 
        string propertyName,
        string assignmentOrReturn, 
        string sourceBuffer, 
        string offset, 
        bool bCleanupSourceBuffer, 
        bool reuseRefMarshallers)
    {
        if (property.IsBitfield)
        {
            builder.AppendLine($"{assignmentOrReturn} {GetMarshaller(property)}.FromNative(IntPtr.Add({sourceBuffer}, {offset}), {GetFieldMaskFieldName(propertyName)});");
            return;
        }
        
        base.ExportFromNative(builder, property, propertyName, assignmentOrReturn, sourceBuffer, offset, bCleanupSourceBuffer, reuseRefMarshallers);
    }
    
    private string GetOffsetFieldName(string nativePropertyName)
    {
        return $"{nativePropertyName}{OffSetPostfix}";
    }
    
    private string GetFieldMaskFieldName(string nativePropertyName)
    {
        return $"{nativePropertyName}{FieldMaskPostfix}";
    }
}