using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class SetPropertyTranslator : PropertyTranslator
{
    public SetPropertyTranslator() : base(ContainerSupportedUsages)
    {
    }

    private string GetWrapperType(UhtProperty property)
    {
        bool isStructProperty = property.IsOuter<UhtScriptStruct>();
        bool isParameter = property.IsOuter<UhtFunction>();
        UhtSetProperty arrayProperty = (UhtSetProperty) property;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(arrayProperty.ValueProperty)!;
        string arrayType = isStructProperty || isParameter ? "SetCopyMarshaller" 
            : property.PropertyFlags.HasAnyFlags(EPropertyFlags.BlueprintReadOnly) ? "SetReadOnlyMarshaller" : "SetMarshaller";

        return $"{arrayType}<{translator.GetManagedType(arrayProperty.ValueProperty)}>";
    }
    
    public override bool CanExport(UhtProperty property)
    {
        return property is UhtSetProperty;
    }

    public override string GetManagedType(UhtProperty property)
    {
        UhtSetProperty setProperty = (UhtSetProperty) property;
        PropertyTranslator keyTranslator = PropertyTranslatorManager.GetTranslator(setProperty.ValueProperty)!;
        string elementType = keyTranslator.GetManagedType(setProperty.ValueProperty);
        
        string interfaceType = property.HasAnyFlags(EPropertyFlags.BlueprintReadOnly) ? "IReadOnlySet" : "ISet";
        return $"System.Collections.Generic.{interfaceType}<{elementType}>";
    }

    public override void ExportPropertyGetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        UhtSetProperty setProperty = (UhtSetProperty) property;
        PropertyTranslator keyTranslator = PropertyTranslatorManager.GetTranslator(setProperty.ValueProperty)!;
        
        string elementType = keyTranslator.GetManagedType(setProperty.ValueProperty);
        string wrapperType = GetWrapperType(property);
        
        builder.AppendLine($"{propertyManagedName}_Marshaller ??= new {wrapperType}(1, {propertyManagedName}_NativeProperty);");
        builder.AppendLine($"return {propertyManagedName}_Marshaller.FromNative(IntPtr.Add(NativeObject, {propertyManagedName}_Offset), 0);");
    }

    public override void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string propertyEngineName)
    {
        base.ExportPropertyVariables(builder, property, propertyEngineName);
        builder.AppendLine($"static IntPtr {propertyEngineName}_NativeProperty;");

        string wrapperType = GetWrapperType(property);
        if (property.IsOuter<UhtScriptStruct>())
        {
            builder.AppendLine($"static {wrapperType} {propertyEngineName}_Marshaller = null;");
        }
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
        return "null";
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