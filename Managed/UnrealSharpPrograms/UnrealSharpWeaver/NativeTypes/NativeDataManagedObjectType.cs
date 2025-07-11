using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataManagedObjectType(TypeReference managedType, int arrayDim) : NativeDataSimpleType(managedType, "ManagedObjectMarshaller`1", arrayDim, PropertyType.Struct)
{
    public TypeReferenceMetadata InnerType { get; set; } =  new(WeaverImporter.Instance.ManagedObjectHandle.Resolve());
}