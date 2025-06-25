using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

internal class NativeDataOptionalType(TypeReference propertyTypeRef, TypeReference innerTypeReference, int arrayDim)
    : NativeDataGenericObjectType(propertyTypeRef, innerTypeReference, "OptionalMarshaller`1", arrayDim, PropertyType.Optional);