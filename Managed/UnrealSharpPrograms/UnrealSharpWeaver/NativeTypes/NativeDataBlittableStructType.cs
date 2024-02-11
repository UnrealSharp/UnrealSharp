using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataBlittableStructType(TypeReference structType, int arrayDim) : NativeDataBlittableStructTypeBase(structType, arrayDim);