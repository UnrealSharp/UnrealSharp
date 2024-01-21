using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataBuiltinType(TypeReference typeRef, string unrealClass, int arrayDim, PropertyType propertyType) 
    : NativeDataSimpleType(typeRef, "BlittableMarshaller`1", unrealClass, arrayDim, propertyType)
{ 
    public override bool IsBlittable => true;
}
