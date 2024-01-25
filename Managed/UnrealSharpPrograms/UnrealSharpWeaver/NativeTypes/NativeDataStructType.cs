using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataStructType(TypeReference structType, string marshallerName, int arrayDim, string unrealPropertyName = "StructProperty", PropertyType propertyType = PropertyType.Struct) 
    : NativeDataSimpleType(structType, marshallerName, unrealPropertyName, arrayDim, propertyType)
{
    public TypeReferenceMetadata InnerType { get; set; } = new(structType.Resolve());
}