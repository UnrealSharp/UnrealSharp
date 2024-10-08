using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataNativeArrayType(TypeReference typeRef, int containerDim, TypeReference innerType)
    : NativeDataContainerType(typeRef, containerDim, PropertyType.Array, innerType)
{
    public override string GetContainerMarshallerName()
    {
        return "NativeArrayMarshaller`1";
    }

    public override string GetCopyContainerMarshallerName()
    {
        return "NativeArrayCopyMarshaller`1";
    }

    public override string GetContainerWrapperType()
    {
        return "System.ReadOnlySpan`1";
    }

    public override void EmitDynamicArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type) { }
}