using Mono.Cecil;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataClassBaseType(TypeReference typeRef, TypeReference innerTypeReference, int arrayDim, string marshallerClass, PropertyType propertyType)
    : NativeDataGenericObjectType(typeRef, innerTypeReference, marshallerClass, arrayDim, propertyType)
{
    protected override TypeReference[] GetTypeParams()
    {
        return [InnerType.TypeRef.ImportType()];
    }
};