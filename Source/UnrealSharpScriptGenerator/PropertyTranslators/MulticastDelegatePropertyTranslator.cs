using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Exporters;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class MulticastDelegatePropertyTranslator : DelegateBasePropertyTranslator
{
    public MulticastDelegatePropertyTranslator() : base(EPropertyUsageFlags.Property)
    {
    }

    public override void OnPropertyExported(GeneratorStringBuilder builder, UhtProperty property)
    {
        UhtMulticastDelegateProperty multicastDelegateProperty = (UhtMulticastDelegateProperty) property;
        DelegateExporter.ExportDelegate(multicastDelegateProperty.Function);
    }

    private string GetBackingField(UhtProperty property)
    {
        return $"{property.SourceName}_BackingField";
    }

    public override bool CanExport(UhtProperty property)
    {
        UhtMulticastDelegateProperty multicastDelegateProperty = (UhtMulticastDelegateProperty) property;
        return ScriptGeneratorUtilities.CanExportFunction(multicastDelegateProperty.Function);
    }

    public override string GetManagedType(UhtProperty property)
    {
        UhtMulticastDelegateProperty multicastDelegateProperty = (UhtMulticastDelegateProperty) property;
        return GetDelegateName(multicastDelegateProperty.Function);
    }

    public override void ExportPropertyStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        base.ExportPropertyStaticConstructor(builder, property, nativePropertyName);
        
        UhtMulticastDelegateProperty multicastDelegateProperty = (UhtMulticastDelegateProperty) property;
        if (multicastDelegateProperty.Function.HasParameters)
        {
            string delegateName = GetDelegateName(multicastDelegateProperty.Function);
            string delegateNamespace = ScriptGeneratorUtilities.GetNamespace(multicastDelegateProperty.Function);
            builder.AppendLine($"{delegateNamespace}.{delegateName}.InitializeUnrealDelegate({nativePropertyName}_NativeProperty);");
        }
    }
    
    public override void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        base.ExportPropertyVariables(builder, property, nativePropertyName);
        
        AddNativePropertyField(builder, nativePropertyName);
        string backingField = GetBackingField(property);
        string delegateName = GetDelegateName(((UhtMulticastDelegateProperty) property).Function);
        builder.AppendLine($"private {delegateName} {backingField}");
    }

    public override void ExportPropertySetter(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        base.ExportPropertySetter(builder, property, nativePropertyName);
        string backingField = GetBackingField(property);
        string delegateName = GetDelegateName(((UhtMulticastDelegateProperty) property).Function);
        
        builder.AppendLine($"if (value == {backingField})");
        builder.OpenBrace();
        builder.AppendLine("return;");
        builder.CloseBrace();
        builder.AppendLine($"{backingField} = value;");
        builder.AppendLine($"DelegateMarshaller<{delegateName}>.ToNative(IntPtr.Add(NativeObject, {property}_Offset), 0, value);");
    }

    public override void ExportPropertyGetter(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        base.ExportPropertyGetter(builder, property, nativePropertyName);
        string backingField = GetBackingField(property);
        string propertyFieldName = GetNativePropertyField(nativePropertyName);
        string delegateName = GetDelegateName(((UhtMulticastDelegateProperty) property).Function);
        
        builder.AppendLine($"if ({backingField} == null)");
        builder.OpenBrace();
        builder.AppendLine($"{backingField} = DelegateMarshaller<{delegateName}>.FromNative(IntPtr.Add(NativeObject, {nativePropertyName}_Offset), {propertyFieldName}, 0);");
        builder.CloseBrace();
        builder.AppendLine($"return {backingField};");
    }

    public override string GetNullValue(UhtProperty property)
    {
        return "null";
    }
}