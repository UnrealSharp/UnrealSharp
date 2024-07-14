using System;
using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Exporters;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class DelegateBasePropertyTranslator : PropertyTranslator
{
    public DelegateBasePropertyTranslator(EPropertyUsageFlags supportedPropertyUsage) : base(supportedPropertyUsage)
    {
    }

    public static string GetDelegateName(UhtFunction function)
    {
        string delegateName = function.EngineName;
        int delegateSignatureIndex = delegateName.IndexOf("__DelegateSignature", StringComparison.Ordinal);
        string strippedDelegateName = delegateName.Substring(0, delegateSignatureIndex);
        return strippedDelegateName;
    }
    
    public static string GetFullDelegateName(UhtFunction function)
    {
        return $"{function.GetNamespace()}.{GetDelegateName(function)}";
    }

    public override void ExportPropertyStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        builder.AppendLine($"{nativePropertyName}_NativeProperty = {ExporterCallbacks.FPropertyCallbacks}.CallGetNativePropertyFromName(NativeClassPtr, \"{property.EngineName}\");");
        base.ExportPropertyStaticConstructor(builder, property, nativePropertyName);
    }

    public override void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string propertyEngineName)
    {
        builder.AppendLine($"static readonly IntPtr {propertyEngineName}_NativeProperty;");
        base.ExportPropertyVariables(builder, property, propertyEngineName);
    }

    public override void ExportParameterVariables(GeneratorStringBuilder builder, UhtFunction function,
        string nativeMethodName, UhtProperty property, string propertyEngineName)
    {
        base.ExportParameterVariables(builder, function, nativeMethodName, property, propertyEngineName);
    }
    
    public override bool CanExport(UhtProperty property)
    {
        throw new System.NotImplementedException();
    }

    public override string GetManagedType(UhtProperty property)
    {
        throw new System.NotImplementedException();
    }

    public override string GetMarshaller(UhtProperty property)
    {
        throw new System.NotImplementedException();
    }

    public override string ExportMarshallerDelegates(UhtProperty property)
    {
        throw new System.NotImplementedException();
    }

    public override string GetNullValue(UhtProperty property)
    {
        throw new System.NotImplementedException();
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string assignmentOrReturn, string sourceBuffer, string offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers)
    {
        throw new System.NotImplementedException();
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer,
        string offset, string source)
    {
        throw new System.NotImplementedException();
    }

    public override string ConvertCPPDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter)
    {
        throw new System.NotImplementedException();
    }
}