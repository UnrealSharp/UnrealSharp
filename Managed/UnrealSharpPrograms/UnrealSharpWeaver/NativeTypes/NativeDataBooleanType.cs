using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataBooleanType(TypeReference typeRef, string unrealClass, int arrayDim) : NativeDataSimpleType(typeRef, "BoolMarshaller", arrayDim, PropertyType.Bool)
{
    public override bool IsPlainOldData => false;
}