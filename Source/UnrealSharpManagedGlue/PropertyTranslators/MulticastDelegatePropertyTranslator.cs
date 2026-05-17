using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class MulticastDelegatePropertyTranslator : DelegateBasePropertyTranslator
{
    public MulticastDelegatePropertyTranslator() : base(EPropertyUsageFlags.Property)
    {
    }

    private string GetBackingField(UhtProperty property)
    {
        return $"{property.SourceName}_BackingField";
    }

    public override bool CanExport(UhtProperty property)
    {
        UhtMulticastDelegateProperty multicastDelegateProperty = (UhtMulticastDelegateProperty) property;
        return multicastDelegateProperty.Function.CanExportParameters();
    }

    public override string GetManagedType(UhtProperty property)
    {
        UhtMulticastDelegateProperty multicastDelegateProperty = (UhtMulticastDelegateProperty) property;
        return $"TMulticastDelegate<{GetFullDelegateName(multicastDelegateProperty.Function)}>";
    }
    
    public override void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string propertyEngineName)
    {
        base.ExportPropertyVariables(builder, property, propertyEngineName);
        string backingField = GetBackingField(property);
        string managedType = GetManagedType(property);
        builder.AppendLine($"private {managedType}? {backingField} = null;");
    }

    public override void ExportPropertySetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        string backingField = GetBackingField(property);
        
        UhtMulticastDelegateProperty multicastDelegateProperty = (UhtMulticastDelegateProperty) property;
        string fullDelegateName = GetFullDelegateName(multicastDelegateProperty.Function);
        
        builder.AppendLine($"if (value == {backingField})");
        builder.OpenBrace();
        builder.AppendLine("return;");
        builder.CloseBrace();
        builder.AppendLine($"{backingField} = value;");
        builder.AppendLine($"MulticastDelegateMarshaller<{fullDelegateName}>.ToNative(NativeObject + {propertyManagedName}_Offset, 0, value);");
    }

    public override void ExportPropertyGetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        string backingField = GetBackingField(property);
        string propertyFieldName = GetNativePropertyField(propertyManagedName);
        
        UhtMulticastDelegateProperty multicastDelegateProperty = (UhtMulticastDelegateProperty) property;
        string fullDelegateName = GetFullDelegateName(multicastDelegateProperty.Function);
        
        builder.AppendLine($"{backingField} ??= MulticastDelegateMarshaller<{fullDelegateName}>.FromNative(NativeObject + {propertyManagedName}_Offset, 0, {propertyFieldName});");
        builder.AppendLine($"return {backingField};");
    }

    public override string GetNullValue(UhtProperty property)
    {
        return "null";
    }
}