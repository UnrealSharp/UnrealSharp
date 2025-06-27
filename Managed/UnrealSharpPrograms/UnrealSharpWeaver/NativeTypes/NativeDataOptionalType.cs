using Mono.Cecil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.NativeTypes;

internal class NativeDataOptionalType(TypeReference propertyTypeRef, TypeReference innerTypeReference, int arrayDim)
    : NativeDataContainerType(propertyTypeRef, arrayDim, PropertyType.Optional, innerTypeReference)
    {
    public override string GetContainerMarshallerName()
    {
        return "OptionalMarshaller`1";
    }

    public override string GetCopyContainerMarshallerName()
    {
        return "OptionalMarshaller`1";
    }
}