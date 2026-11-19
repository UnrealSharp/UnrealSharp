using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class SinglecastDelegatePropertyTranslator : DelegateBasePropertyTranslator
{
    public SinglecastDelegatePropertyTranslator() : base(EPropertyUsageFlags.Property | EPropertyUsageFlags.Parameter)
    {
    }

    private string GetBackingField(UhtProperty property)
    {
        return $"{property.SourceName}_BackingField";
    }
    
    public override bool CanExport(UhtProperty property)
    {
        UhtDelegateProperty delegateProperty = (UhtDelegateProperty) property;
        bool hasReturnValue = delegateProperty.Function.ReturnProperty != null;
        return delegateProperty.Function.CanExportParameters() && !hasReturnValue;
    }
    
    public override string GetManagedType(UhtProperty property)
    {
        return $"TDelegate<{GetFullDelegateName(((UhtDelegateProperty) property).Function)}>";
    }

    public override void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string propertyEngineName)
    {
        base.ExportPropertyVariables(builder, property, propertyEngineName);
        builder.AppendLine($"private {GetManagedType(property)}? {GetBackingField(property)} = null;");
    }

    public override void ExportPropertySetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        string backingField = GetBackingField(property);

        builder.AppendLine($"if (value == {backingField})");
        builder.OpenBrace();
        builder.AppendLine("return;");
        builder.CloseBrace();

        ExportToNative(builder, property, propertyManagedName, "NativeObject", $"{propertyManagedName}_Offset", "value", false);
        builder.AppendLine($"{backingField} = null;");
    }

    public override void ExportPropertyGetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        string backingField = GetBackingField(property);
        string propertyFieldName = GetNativePropertyField(propertyManagedName);
        string fullDelegateName = GetFullDelegateName(((UhtDelegateProperty) property).Function);

        builder.AppendLine($"{backingField} ??= SingleDelegateMarshaller<{fullDelegateName}>.FromNative(NativeObject + {propertyManagedName}_Offset, 0, {propertyFieldName});");
        builder.AppendLine($"return {backingField};");
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string destinationBuffer,
        string offset, string source, bool reuseRefMarshallers)
    {
        UhtDelegateProperty delegateProperty = (UhtDelegateProperty) property;
        string fullDelegateName = GetFullDelegateName(delegateProperty.Function);
        builder.AppendLine($"SingleDelegateMarshaller<{fullDelegateName}>.ToNative({destinationBuffer} + {offset}, 0, {source});");
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string assignmentOrReturn, string sourceBuffer, string offset, bool cleanupSourceBuffer,
        bool reuseRefMarshallers)
    {
        UhtDelegateProperty delegateProperty = (UhtDelegateProperty) property;
        string fullDelegateName = GetFullDelegateName(delegateProperty.Function);
        builder.AppendLine($"{assignmentOrReturn} SingleDelegateMarshaller<{fullDelegateName}>.FromNative({sourceBuffer} + {offset}, 0, IntPtr.Zero);");
    }

    public override string GetNullValue(UhtProperty property)
    {
        return "null";
    }

    public override void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property, string paramName)
    {
        
    }
}
