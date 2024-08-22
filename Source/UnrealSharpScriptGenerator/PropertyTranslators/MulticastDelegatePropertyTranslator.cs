using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

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
        return ScriptGeneratorUtilities.CanExportParameters(multicastDelegateProperty.Function);
    }

    public override string GetManagedType(UhtProperty property)
    {
        UhtMulticastDelegateProperty multicastDelegateProperty = (UhtMulticastDelegateProperty) property;
        return GetFullDelegateName(multicastDelegateProperty.Function);
    }

    public override void ExportPropertyStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        base.ExportPropertyStaticConstructor(builder, property, nativePropertyName);
        
        UhtMulticastDelegateProperty multicastDelegateProperty = (UhtMulticastDelegateProperty) property;
        if (multicastDelegateProperty.Function.HasParameters)
        {
            string fullDelegateName = GetFullDelegateName(((UhtMulticastDelegateProperty) property).Function);
            builder.AppendLine($"{fullDelegateName}.InitializeUnrealDelegate({nativePropertyName}_NativeProperty);");
        }
    }
    
    public override void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string propertyEngineName)
    {
        base.ExportPropertyVariables(builder, property, propertyEngineName);
        string backingField = GetBackingField(property);
        string fullDelegateName = GetManagedType(property);
        builder.AppendLine($"private {fullDelegateName} {backingField};");
    }

    public override void ExportPropertySetter(GeneratorStringBuilder builder, UhtProperty property,
        string propertyManagedName)
    {
        string backingField = GetBackingField(property);
        string fullDelegateName = GetManagedType(property);
        
        builder.AppendLine($"if (value == {backingField})");
        builder.OpenBrace();
        builder.AppendLine("return;");
        builder.CloseBrace();
        builder.AppendLine($"{backingField} = value;");
        builder.AppendLine($"DelegateMarshaller<{fullDelegateName}>.ToNative(IntPtr.Add(NativeObject, {propertyManagedName}_Offset), 0, value);");
    }

    public override void ExportPropertyGetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        string backingField = GetBackingField(property);
        string propertyFieldName = GetNativePropertyField(propertyManagedName);
        string fullDelegateName = GetManagedType(property);
        
        builder.AppendLine($"if ({backingField} == null)");
        builder.OpenBrace();
        builder.AppendLine($"{backingField} = DelegateMarshaller<{fullDelegateName}>.FromNative(IntPtr.Add(NativeObject, {propertyManagedName}_Offset), {propertyFieldName}, 0);");
        builder.CloseBrace();
        builder.AppendLine($"return {backingField};");
    }

    public override string GetNullValue(UhtProperty property)
    {
        return "null";
    }
}