using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataNameType(TypeReference structType, int arrayDim) : NativeDataBlittableStructTypeBase(structType, arrayDim, PropertyType.Name);