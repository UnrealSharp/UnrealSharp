using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnrealSharpWeaver;

[Serializable]
class InvalidConstructorException(MethodDefinition constructor, string message) : WeaverProcessError(message, ErrorEmitter.GetSequencePointFromMemberDefinition(constructor));

[Serializable]
class ConstructorNotFoundException(TypeDefinition type, string message) : WeaverProcessError(message, ErrorEmitter.GetSequencePointFromMemberDefinition(type));

[Serializable]
class InvalidUnrealClassException(string propertyName, SequencePoint? sequencePoint, string message) : WeaverProcessError($"Class '{propertyName}' is invalid as a unreal class: {message}",
        sequencePoint)
{
    public InvalidUnrealClassException(TypeDefinition klass, string message)
        : this(klass.FullName, ErrorEmitter.GetSequencePointFromMemberDefinition(klass), message)
    {
    }
}

[Serializable]
class InvalidUnrealStructException(TypeDefinition structType, string message) 
    : WeaverProcessError($"Struct '{structType.FullName}' is invalid as Unreal struct: {message}", ErrorEmitter.GetSequencePointFromMemberDefinition(structType));

[Serializable]
class InvalidUnrealEnumException(TypeDefinition enumType, string message) : WeaverProcessError(
    $"Enum '{enumType.FullName}' is invalid as Unreal enum: {message}",
    ErrorEmitter.GetSequencePointFromMemberDefinition(enumType));

[Serializable]
class InvalidPropertyException(string propertyName, SequencePoint? sequencePoint, string message)
    : WeaverProcessError($"Property '{propertyName}' is invalid for unreal property: {message}",
        sequencePoint)
{
    public InvalidPropertyException(IMemberDefinition property, string message)
        : this(property.FullName, ErrorEmitter.GetSequencePointFromMemberDefinition(property), message)
    {
    }
}


[Serializable]
class InvalidUnrealFunctionException(MethodDefinition method, string message)
    : WeaverProcessError($"Method '{method.Name}' is invalid for unreal function: {message}", null,
        ErrorEmitter.GetSequencePointFromMemberDefinition(method));

[Serializable]
class NotDerivableClassException(TypeDefinition klass, TypeDefinition superKlass) : WeaverProcessError(
    $"Class '{klass.FullName}' is invalid because '{superKlass.FullName}' may not be derived from in managed code.",
    ErrorEmitter.GetSequencePointFromMemberDefinition(klass));

[Serializable]
class UnableToFixPropertyBackingReferenceException : WeaverProcessError
{
    public UnableToFixPropertyBackingReferenceException(MethodDefinition constructor, PropertyDefinition property, OpCode opCode)
        : base($"The type {constructor.DeclaringType.FullName}'s constructor references the property {property.Name} using an unsupported IL pattern", ErrorEmitter.GetSequencePointFromMemberDefinition(constructor))
    {
    }
}

[Serializable]
class UnsupportedPropertyInitializerException(PropertyDefinition property) : WeaverProcessError($"Property initializer for UProperty {property.Name} is not a supported constant type", ErrorEmitter.GetSequencePointFromMemberDefinition(property));

[Serializable]
class RewriteException(TypeDefinition type, string message) : WeaverProcessError($"{type.FullName}: {message}", ErrorEmitter.GetSequencePointFromMemberDefinition(type));

[Serializable]
class InvalidAttributeException(TypeDefinition attributeType, SequencePoint? point, string message) : WeaverProcessError($"Invalid attribute class {attributeType.Name}: {message}", point);