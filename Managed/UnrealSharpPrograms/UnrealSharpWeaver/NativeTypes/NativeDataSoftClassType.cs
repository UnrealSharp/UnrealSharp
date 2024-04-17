using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataSoftClassType(TypeReference typeRef, TypeReference innerTypeReference, int arrayDim) 
    : NativeDataGenericObjectType(typeRef, innerTypeReference, "BlittableMarshaller`1", arrayDim, PropertyType.SoftClass);