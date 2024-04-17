using Mono.Cecil;

namespace UnrealSharpWeaver.MetaData;

public class VirtualFunctionMetaData(MethodDefinition method) : FunctionMetaData(method);