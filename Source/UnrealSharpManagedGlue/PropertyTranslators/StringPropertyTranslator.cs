using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class StringPropertyTranslator : PropertyTranslator
{
    public StringPropertyTranslator() : base(EPropertyUsageFlags.Any)
    {
    }
    
    public override bool CacheProperty => true;
    public override bool CanExport(UhtProperty property) => property is UhtStrProperty;

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

    public override void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property, string paramName)
    {
        builder.AppendLine($"StringMarshaller.DestructInstance({paramName}_NativePtr, 0);");
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string assignmentOrReturn,
        string sourceBuffer, string offset, bool cleanupSourceBuffer, bool reuseRefMarshallers)
    {
        if (!reuseRefMarshallers)
        {
            builder.AppendLine($"IntPtr {propertyName}_NativePtr = {sourceBuffer} + {offset};");
        }
        
        builder.AppendLine($"{assignmentOrReturn} StringMarshaller.FromNative({propertyName}_NativePtr,0);");
    }

    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer, string offset, string source)
    {
        builder.AppendLine($"IntPtr {propertyName}_NativePtr = {destinationBuffer} + {offset};");
        builder.AppendLine($"StringMarshaller.ToNative({propertyName}_NativePtr,0,{source});");
    }

    public override string ConvertCppDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter)
    {
        return "\"" + defaultValue + "\"";
    }

    public override bool CanSupportGenericType(UhtProperty property) => false;
}