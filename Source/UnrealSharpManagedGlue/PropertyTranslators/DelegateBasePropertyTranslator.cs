using System;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class DelegateBasePropertyTranslator : PropertyTranslator
{
    public DelegateBasePropertyTranslator(EPropertyUsageFlags supportedPropertyUsage) : base(supportedPropertyUsage)
    {
    }
    
    public override bool CacheProperty => true;

    private const string DelegateSignatureSuffix = "__DelegateSignature";
    private const string StructPrefix = "F";

    public static string GetDelegateName(UhtFunction function)
    {
        string engineName = function.EngineName;
        int suffixIndex = engineName.IndexOf(DelegateSignatureSuffix, StringComparison.Ordinal);
        
        if (suffixIndex < 0)
        {
            throw new InvalidOperationException($"Function '{engineName}' is not a delegate signature.");
        }

        return StructPrefix + engineName.Substring(0, suffixIndex);
    }
    
    public static string GetFullDelegateName(UhtFunction function)
    {
        return $"{function.GetNamespace()}.{GetDelegateName(function)}";
    }

    public static string GetFullWrapperName(UhtFunction function)
    {
        return $"{function.GetNamespace()}.{GetWrapperName(function)}";
    }
    
    public static string GetWrapperName(UhtFunction function)
    {
        return $"{StructPrefix}{function.EngineName}";
    }
    
    public override bool CanExport(UhtProperty property)
    {
        throw new NotImplementedException();
    }

    public override string GetManagedType(UhtProperty property)
    {
        throw new NotImplementedException();
    }

    public override string GetMarshaller(UhtProperty property)
    {
        throw new NotImplementedException();
    }

    public override string ExportMarshallerDelegates(UhtProperty property)
    {
        throw new NotImplementedException();
    }

    public override string GetNullValue(UhtProperty property)
    {
        throw new NotImplementedException();
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string assignmentOrReturn, string sourceBuffer, string offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers)
    {
        throw new NotImplementedException();
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer,
        string offset, string source)
    {
        throw new NotImplementedException();
    }

    public override string ConvertCPPDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter)
    {
        throw new NotImplementedException();
    }

    public override bool CanSupportGenericType(UhtProperty property) => false;
}