using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataArrayType(TypeReference typeRef, int containerDim, TypeReference innerType) 
    : NativeDataContainerType(typeRef, containerDim, PropertyType.Array, innerType)
{
    public override string GetContainerMarshallerName()
    {
        return "ArrayMarshaller`1";
    }

    public override string GetCopyContainerMarshallerName()
    {
        return "ArrayCopyMarshaller`1";
    }

    public override string GetContainerWrapperType()
    {
        return "System.Collections.Generic.IList`1";
    }
}