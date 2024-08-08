using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class SinglecastDelegatePropertyTranslator : DelegateBasePropertyTranslator
{
    public SinglecastDelegatePropertyTranslator() : base(EPropertyUsageFlags.Parameter)
    {
    }
    
    public override bool CanExport(UhtProperty property)
    {
        UhtDelegateProperty delegateProperty = (UhtDelegateProperty) property;
        bool hasReturnValue = delegateProperty.Function.ReturnProperty != null;
        return ScriptGeneratorUtilities.CanExportParameters(delegateProperty.Function) && !hasReturnValue;
    }
    
    public override string GetManagedType(UhtProperty property)
    {
        return GetFullDelegateName(((UhtDelegateProperty) property).Function);
    }

    public override void ExportPropertyStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        base.ExportPropertyStaticConstructor(builder, property, nativePropertyName);
        
        UhtDelegateProperty delegateProperty = (UhtDelegateProperty) property;
        if (delegateProperty.Function.HasParameters)
        {
            string fullDelegateName = GetManagedType(property);
            builder.AppendLine($"{fullDelegateName}.InitializeUnrealDelegate({nativePropertyName}_NativeProperty);");
        }
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer,
        string offset, string source)
    {
        string fullDelegateName = GetManagedType(property);
        builder.AppendLine($"DelegateMarshaller<{fullDelegateName}>.ToNative(IntPtr.Add({destinationBuffer}, {offset}), 0, {source});");
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string assignmentOrReturn, string sourceBuffer, string offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers)
    {
        string fullDelegateName = GetManagedType(property);
        builder.AppendLine($"{assignmentOrReturn} DelegateMarshaller<{fullDelegateName}>.FromNative(IntPtr.Add({sourceBuffer}, {offset}), IntPtr.Zero, 0);");
    }

    public override string GetNullValue(UhtProperty property)
    {
        return "null";
    }

    public override void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property,
        string paramName)
    {
        
    }
}