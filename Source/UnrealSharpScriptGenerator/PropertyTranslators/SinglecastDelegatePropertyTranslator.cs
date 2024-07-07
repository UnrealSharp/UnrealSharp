using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Exporters;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class SinglecastDelegatePropertyTranslator : DelegateBasePropertyTranslator
{
    public SinglecastDelegatePropertyTranslator() : base(EPropertyUsageFlags.Parameter)
    {
    }
    
    public override void OnPropertyExported(GeneratorStringBuilder builder, UhtProperty property)
    {
        UhtDelegateProperty multicastDelegateProperty = (UhtDelegateProperty) property;
        DelegateExporter.ExportDelegate(multicastDelegateProperty.Function);
    }
    
    public override bool CanExport(UhtProperty property)
    {
        UhtDelegateProperty delegateProperty = (UhtDelegateProperty) property;
        return ScriptGeneratorUtilities.CanExportFunction(delegateProperty.Function);
    }

    public override string GetManagedType(UhtProperty property)
    {
        return GetDelegateName(((UhtDelegateProperty) property).Function);
    }

    public override void ExportPropertyStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        base.ExportPropertyStaticConstructor(builder, property, nativePropertyName);
        
        UhtDelegateProperty delegateProperty = (UhtDelegateProperty) property;
        if (delegateProperty.Function.HasParameters)
        {
            string delegateName = GetDelegateName(delegateProperty.Function);
            string delegateNamespace = ScriptGeneratorUtilities.GetNamespace(delegateProperty.Function);
            builder.AppendLine($"{delegateNamespace}.{delegateName}.InitializeUnrealDelegate({nativePropertyName}_NativeProperty);");
        }
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer,
        string offset, string source)
    {
        string delegateName = GetDelegateName(((UhtDelegateProperty) property).Function);
        builder.AppendLine($"DelegateMarshaller<{delegateName}>.ToNative(IntPtr.Add({destinationBuffer}, {offset}), 0, {source});");
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string assignmentOrReturn, string sourceBuffer, string offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers)
    {
        string delegateName = GetDelegateName(((UhtDelegateProperty) property).Function);
        builder.AppendLine($"{assignmentOrReturn} DelegateMarshaller<{delegateName}>.FromNative(IntPtr.Add({sourceBuffer}, {offset}), IntPtr.Zero, 0);");
    }

    public override string GetNullValue(UhtProperty property)
    {
        return "null";
    }

    public override void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property, string paramName)
    {
        
    }
}