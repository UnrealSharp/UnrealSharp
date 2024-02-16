using Mono.Cecil;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataTextType(TypeReference textType) : NativeDataGenericObjectType(textType, textType, "TextMarshaller", 1, PropertyType.Text);