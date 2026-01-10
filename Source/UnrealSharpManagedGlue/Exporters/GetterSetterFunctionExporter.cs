using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.PropertyTranslators;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.Exporters;

public enum GetterSetterMode
{
    Get,
    Set
}

public class GetterSetterFunctionExporter : FunctionExporter
{
    private readonly UhtProperty _propertyGetterSetter;
    private readonly GetterSetterMode _getterSetterMode;
    private string _outParameterName;
    
    public static GetterSetterFunctionExporter Create(UhtFunction function, 
        UhtProperty propertyGetterSetter, 
        GetterSetterMode getterSetterMode, 
        EFunctionProtectionMode protectionMode)
    {
        GetterSetterFunctionExporter exporter = new GetterSetterFunctionExporter(function, propertyGetterSetter, getterSetterMode);
        exporter.Initialize(OverloadMode.SuppressOverloads, protectionMode, EBlueprintVisibility.GetterSetter);
        return exporter;
    }
    
    private GetterSetterFunctionExporter(UhtFunction function, UhtProperty propertyGetterSetter, GetterSetterMode getterSetterMode) : base(function)
    {
        _outParameterName = string.Empty;
        _propertyGetterSetter = propertyGetterSetter;
        _getterSetterMode = getterSetterMode;
        
        Initialize(OverloadMode.SuppressOverloads, EFunctionProtectionMode.OverrideWithInternal, EBlueprintVisibility.GetterSetter);
    }

    protected override string GetParameterName(UhtProperty parameter)
    {
        return _getterSetterMode == GetterSetterMode.Get ? _propertyGetterSetter.GetParameterName() : "value";
    }

    protected override string MakeOutMarshalDestination(UhtProperty parameter, PropertyTranslator propertyTranslator, GeneratorStringBuilder builder)
    {
        _outParameterName = GetParameterName(parameter) + "_Out";
        builder.AppendLine($"{propertyTranslator.GetManagedType(parameter)} {_outParameterName};");
        return _outParameterName;
    }

    protected override void ExportReturnStatement(GeneratorStringBuilder builder)
    {
        if (Function.ReturnProperty != null && Function.ReturnProperty.IsSameType(_propertyGetterSetter))
        {
            string castOperation = _propertyGetterSetter.HasAllFlags(EPropertyFlags.BlueprintReadOnly) 
                ? $"({ReturnValueTranslator!.GetManagedType(_propertyGetterSetter)})" : string.Empty;
            builder.AppendLine($"return {castOperation}returnValue;");
        }
        else if (Function.ReturnProperty != null)
        {
            // Types differ (e.g., getter returns FText, property bound as string). Still return and rely on
            // available implicit/user-defined conversions on the managed types (FText -> string, etc.).
            builder.AppendLine("return returnValue;");
        }
        
        if (string.IsNullOrEmpty(_outParameterName))
        {
            return;
        }
        
        builder.AppendLine($"return {_outParameterName};");
    }
}
