using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataUnmanagedType(TypeReference unmanagedType, int arrayDim) : NativeDataSimpleType(unmanagedType, "UnmanagedTypeMarshaller`1", arrayDim, PropertyType.Struct)
{
    public TypeReferenceMetadata InnerType { get; set; } =  new(WeaverImporter.Instance.UnmanagedDataStore.Resolve());
}