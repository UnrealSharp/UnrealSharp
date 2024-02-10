using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataEnumType(TypeReference typeRef, int arrayDim) : NativeDataSimpleType(typeRef, "BlittableMarshaller`1", arrayDim, PropertyType.Enum)
{
    public TypeReferenceMetadata InnerProperty { get; set; } = new(typeRef.Resolve());
};