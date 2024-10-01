using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataTextType(TypeReference textType) : NativeDataSimpleType(textType, "TextMarshaller", 1, PropertyType.Text);