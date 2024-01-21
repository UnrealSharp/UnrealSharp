using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataSoftObjectType(TypeReference typeRef, TypeReference innerTypeReference, int arrayDim)
    : NativeDataGenericObjectType(typeRef, innerTypeReference, "BlittableMarshaller`1", "SoftObjectProperty", arrayDim, PropertyType.SoftObject)
{
}