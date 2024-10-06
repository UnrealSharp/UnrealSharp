using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataClassBaseType(TypeReference typeRef, TypeReference innerTypeReference, int arrayDim, string marshallerClass, PropertyType propertyType)
    : NativeDataGenericObjectType(typeRef, innerTypeReference, marshallerClass, arrayDim, propertyType)
{
    protected override TypeReference[] GetTypeParams()
    {
        return [WeaverHelper.ImportType(InnerType.TypeRef)];
    }
};