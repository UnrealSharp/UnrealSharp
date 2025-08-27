using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataArrayType(TypeReference typeRef, int containerDim, TypeReference innerType) 
    : NativeDataContainerType(typeRef, containerDim, PropertyType.Array, innerType)
{
    public override string GetContainerMarshallerName()
    {
        return "ArrayMarshaller`1";
    }

    public override string GetCopyContainerMarshallerName()
    {
        return "ArrayCopyMarshaller`1";
    }

    public override string GetContainerWrapperType()
    {
        return "System.Collections.Generic.IList`1";
    }

    public override void WriteSetter(TypeDefinition type, MethodDefinition setter, Instruction[] loadBufferPtr,
                                     FieldDefinition? fieldDefinition)
    {
        base.WriteSetter(type, setter, loadBufferPtr, fieldDefinition);
    }
}