using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataSoftObjectType(TypeReference typeRef, TypeReference innerTypeReference, int arrayDim)
    : NativeDataClassBaseType(typeRef, innerTypeReference, arrayDim, "SoftObjectMarshaller`1", PropertyType.SoftObject);