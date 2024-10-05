using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataSoftClassType(TypeReference typeRef, TypeReference innerTypeReference, int arrayDim) 
    : NativeDataClassBaseType(typeRef, innerTypeReference, arrayDim, "SoftClassMarshaller`1", PropertyType.SoftClass);