using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataObjectType(TypeReference propertyTypeRef, TypeReference innerTypeReference, string unrealClass, int arrayDim) 
    : NativeDataGenericObjectType(propertyTypeRef, innerTypeReference, "ObjectMarshaller`1", arrayDim, PropertyType.Object);
