using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;
class NativeDataWeakObjectType(TypeReference typeRef, TypeReference innerTypeRef, int arrayDim) 
    : NativeDataGenericObjectType(typeRef, innerTypeRef, "BlittableMarshaller`1", arrayDim, PropertyType.WeakObject);