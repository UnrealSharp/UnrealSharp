using System;
using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public abstract class PropertyTranslator
{
    EPropertyUsageFlags SupportedPropertyUsage;
    
    public bool IsSupportedAsProperty() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.Property);
    public bool IsSupportedAsParameter() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.Parameter);
    public bool IsSupportedAsReturnValue() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.ReturnValue);
    public bool IsSupportedAsInner() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.Inner);
    public bool IsSupportedAsStructProperty() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.StructProperty);
    public bool IsSupportedAsOverridableFunctionParameter() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.OverridableFunctionParameter);
    public bool IsSupportedAsOverridableFunctionReturnValue() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.OverridableFunctionReturnValue);
    public bool IsSupportedInStaticArray() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.StaticArrayProperty);
    
    public PropertyTranslator(EPropertyUsageFlags supportedPropertyUsage)
    {
        SupportedPropertyUsage = supportedPropertyUsage;
    }
    
    // Can we export this property?
    public abstract bool CanExport(UhtProperty property);
    
    // Get the managed type for this property
    // Example: "int" for a property of type "int32"
    public abstract string GetManagedType(UhtProperty property);
    
    // Get the marshaller for this property to marshal back and forth between C++ and C#
    public abstract string GetMarshaller(UhtProperty property);
    
    // Get the marshaller delegates for this property
    public abstract string ExportMarshallerDelegates(UhtProperty property);
    
    // Get the null value for this property
    public abstract string GetNullValue(UhtProperty property);
    
    // Export the static constructor for this property
    public virtual void ExportPropertyStaticConstructor(StringBuilder builder, UhtProperty property, string nativePropertyName)
    {
       builder.AppendLine($"{nativePropertyName}_Offset = {ExporterCallbacks.FPropertyCallbacks}.CallGetPropertyOffsetFromName(NativeClassPtr, \"{nativePropertyName}\");");
    }
    
    public virtual void ExportParameterStaticConstructor(StringBuilder builder, UhtProperty property, UhtFunction function, string nativePropertyName)
    {
        builder.AppendLine($"{function.SourceName}_{nativePropertyName}_Offset = {ExporterCallbacks.FPropertyCallbacks}.CallGetPropertyOffsetFromName(NativeClassPtr, \"{nativePropertyName}\");");
    }

    public virtual void ExportPropertyVariables(StringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        builder.AppendLine($"static int {nativePropertyName}_Offset;");
    }

    public virtual void ExportPropertyGetter(StringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        throw new NotImplementedException();
    }

    public virtual void ExportPropertySetter(StringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        throw new NotImplementedException();
    }

    public virtual void ExportCppDefaultParameterAsLocalVariable(StringBuilder builder, string variableName,
        string defaultValue, UhtFunction function, UhtProperty paramProperty)
    {
        throw new NotImplementedException();
    }

    public virtual void ExportFunctionReturnStatement(StringBuilder builder,
        UhtProperty property,
        string nativePropertyName, 
        string functionName, 
        string paramsCallString)
    {
        throw new NotImplementedException();
    }
    
    // Cleanup the marshalling buffer
    public virtual void ExportCleanupMarshallingBuffer(StringBuilder builder, UhtProperty property, string paramName)
    {
        throw new NotImplementedException();
    }
    
    // Build the C# code to marshal this property from C++ to C#
    public abstract void ExportFromNative(StringBuilder builder, UhtProperty property, string nativePropertyName,
        string assignmentOrReturn, string sourceBuffer, string offset, bool bCleanupSourceBuffer,
        bool reuseRefMarshallers);
    
    // Build the C# code to marshal this property from C# to C++
    public abstract void ExportToNative(StringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer, string offset, string source);
    
    // Convert a C++ default value to a C# default value
    // Example: "0.0f" for a float property
    public abstract string ConvertCPPDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter);
    
    // Is this property the same memory layout as the C++ type?
    public virtual bool IsBlittable => false;

}