using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataBooleanType(TypeReference typeRef, int arrayDim) : NativeDataSimpleType(typeRef, "BoolMarshaller", arrayDim, PropertyType.Bool)
{
    public override bool IsPlainOldData => false;
}