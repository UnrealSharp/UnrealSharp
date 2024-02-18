using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataInterfaceType(TypeReference typeRef, int arrayDim) 
    : NativeDataSimpleType(typeRef, "InterfaceMarshaller`1", arrayDim, PropertyType.Interface)
{
    
}