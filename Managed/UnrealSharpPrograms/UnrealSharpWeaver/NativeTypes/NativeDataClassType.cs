using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataClassType(TypeReference typeRef, TypeReference innerTypeReference, int arrayDim)
    : NativeDataClassBaseType(typeRef, innerTypeReference, arrayDim, "SubclassOfMarshaller`1", PropertyType.Class);