using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class StringPropertyTranslator : PropertyTranslator
{
    public StringPropertyTranslator() : base(EPropertyUsageFlags.Any)
    {
    }

    public override bool CanExport(UhtProperty property)
    {
        return property is UhtStrProperty;
    }

    public override string GetManagedType(UhtProperty property)
    {
        return "string";
    }

    public override string GetMarshaller(UhtProperty property)
    {
        return "StringMarshaller";
    }

    public override string ExportMarshallerDelegates(UhtProperty property)
    {
        return "StringMarshaller.ToNative, StringMarshaller.FromNative";
    }

    public override string GetNullValue(UhtProperty property)
    {
        return "\"\"";
    }

    public override void ExportPropertyGetter(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        builder.AppendLine($"StringMarshaller.ToNative(IntPtr.Add(NativeObject,{nativePropertyName}_Offset),0,value);");
    }

    public override void ExportPropertySetter(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        builder.AppendLine($"return StringMarshaller.FromNative(IntPtr.Add(NativeObject,{nativePropertyName}_Offset),0);");
    }

    public override void ExportPropertyStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        builder.AppendLine($"{nativePropertyName}_NativeProperty = {ExporterCallbacks.FPropertyCallbacks}.CallGetNativePropertyFromName(NativeClassPtr, \"{nativePropertyName}\");");
    }

    public override void ExportFunctionReturnStatement(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName,
        string functionName, string paramsCallString)
    {
        builder.AppendLine($"return {ExporterCallbacks.FStringCallbacks}.CallConvertTCHARToUTF8(Invoke_{functionName}(NativeObject, {functionName}_NativeFunction{paramsCallString}));");
    }

    public override void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        base.ExportPropertyVariables(builder, property, nativePropertyName);
        builder.AppendLine($"static readonly IntPtr {nativePropertyName}_NativeProperty;");
    }

    public override void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property, string paramName)
    {
        builder.AppendLine($"StringMarshaller.DestructInstance({paramName}_NativePtr, 0);\"");
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string assignmentOrReturn,
        string sourceBuffer, string offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers)
    {
        if (!reuseRefMarshallers)
        {
            builder.AppendLine($"IntPtr {propertyName}_NativePtr = IntPtr.Add({sourceBuffer},{offset});");
        }
        builder.AppendLine($"{assignmentOrReturn} StringMarshaller.FromNative({propertyName}_NativePtr,0);");
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer, string offset, string source)
    {
        builder.AppendLine($"IntPtr {propertyName}_NativePtr = IntPtr.Add({destinationBuffer},%s);");
        builder.AppendLine($"StringMarshaller.ToNative({propertyName}_NativePtr,0,{source});");
    }

    public override string ConvertCPPDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter)
    {
        return "\"" + defaultValue + "\"";
    }
}