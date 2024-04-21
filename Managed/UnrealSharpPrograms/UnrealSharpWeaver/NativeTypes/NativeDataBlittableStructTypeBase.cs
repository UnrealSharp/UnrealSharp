using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataBlittableStructTypeBase(TypeReference structType, int arrayDim, PropertyType propertyType = PropertyType.Struct)
    : NativeDataStructType(structType, "BlittableMarshaller`1", arrayDim, propertyType)
{
    public override bool IsBlittable => true;
}