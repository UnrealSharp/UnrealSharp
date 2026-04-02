using System;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

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
        
        if (suffixIndex == -1)
        {
            return StructPrefix + engineName;
        }
        
        string strippedDelegateName = engineName.Substring(0, suffixIndex);
        
        // If delegate has an Outer (owner class/struct), add Outer name as prefix to delegate name
        // This allows distinguishing delegates with same name but different owner classes (e.g., UComboBoxString::FOnSelectionChangedEvent vs UComboBoxKey::FOnSelectionChangedEvent)
        if (function.Outer != null && function.Outer is not UhtPackage)
        {
            string outerName = function.Outer.SourceName;
            
            // Remove common prefix to keep name concise
            if (outerName.StartsWith("U") && outerName.Length > 1 && char.IsUpper(outerName[1]))
            {
                outerName = outerName.Substring(1);
            }
            
            strippedDelegateName = $"{outerName}_{strippedDelegateName}";
        }
        
        return StructPrefix + strippedDelegateName;
    }
    
    public static string GetFullDelegateName(UhtFunction function)
    {
        return $"{function.GetNamespace()}.{GetDelegateName(function)}";
    }
    
    public static string GetWrapperName(UhtFunction function)
    {
        return $"{GetDelegateName(function)}__DelegateSignature";
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
        string assignmentOrReturn, string sourceBuffer, string offset, bool cleanupSourceBuffer,
        bool reuseRefMarshallers)
    {
        throw new NotImplementedException();
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string destinationBuffer,
        string offset, string source, bool reuseRefMarshallers)
    {
        throw new NotImplementedException();
    }

    public override string ConvertCppDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter)
    {
        throw new NotImplementedException();
    }

    public override bool CanSupportGenericType(UhtProperty property) => false;
}