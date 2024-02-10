using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

abstract class NativeDataGenericObjectType(TypeReference typeRef, TypeReference innerTypeReference, string marshalerClass, string unrealClass, int arrayDim, PropertyType propertyType)
    : NativeDataSimpleType(typeRef, marshalerClass, arrayDim, propertyType)
{
    public TypeReferenceMetadata InnerType { get; set; } = new(innerTypeReference.Resolve());
}