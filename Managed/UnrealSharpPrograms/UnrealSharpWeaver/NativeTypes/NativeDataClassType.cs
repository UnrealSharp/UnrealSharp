using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataClassType(TypeReference typeRef, TypeReference innerTypeReference, int arrayDim)
    : NativeDataGenericObjectType(typeRef, innerTypeReference, "SubclassOfMarshaller`1", arrayDim, PropertyType.Class)
{
    protected override TypeReference[] GetTypeParams()
    {
        return [WeaverHelper.ImportType(InnerType.TypeDef)];
    }
};