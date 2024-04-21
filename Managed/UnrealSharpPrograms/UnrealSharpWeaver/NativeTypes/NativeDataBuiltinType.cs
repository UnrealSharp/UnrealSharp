using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataBuiltinType(TypeReference typeRef, int arrayDim, PropertyType propertyType) : NativeDataSimpleType(typeRef, "BlittableMarshaller`1", arrayDim, propertyType)
{ 
    public override bool IsBlittable => true;
}
