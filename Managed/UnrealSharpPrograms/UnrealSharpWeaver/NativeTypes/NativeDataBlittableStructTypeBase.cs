using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataBlittableStructTypeBase : NativeDataStructType
{
    public NativeDataBlittableStructTypeBase(TypeReference structType, int arrayDim, string unrealPropertyName, PropertyType propertyType = PropertyType.Struct) 
        : base(structType, "BlittableMarshaller`1", arrayDim, unrealPropertyName, propertyType)
    {
        
    }
    public override bool IsBlittable => true;
}