using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

abstract class NativeDataGenericObjectType(TypeReference typeRef, TypeReference innerTypeReference, string marshallerClass, int arrayDim, PropertyType propertyType)
    : NativeDataSimpleType(typeRef, marshallerClass, arrayDim, propertyType)
{
    public TypeReferenceMetadata InnerType { get; set; } = new(innerTypeReference.Resolve());
}