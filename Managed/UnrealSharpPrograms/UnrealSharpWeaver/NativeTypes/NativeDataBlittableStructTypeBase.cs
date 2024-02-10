using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataBlittableStructTypeBase : NativeDataStructType
{
    public NativeDataBlittableStructTypeBase(TypeReference structType, int arrayDim, PropertyType propertyType = PropertyType.Struct) 
        : base(structType, "BlittableMarshaller`1", arrayDim, propertyType)
    {
        
    }
    public override bool IsBlittable => true;
}