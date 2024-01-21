using Mono.Cecil;

namespace UnrealSharpWeaver.MetaData;

public class TypeReferenceMetadata : BaseMetaData
{
    public TypeReferenceMetadata(TypeReference typeReference) : this(typeReference.Resolve()) {}
    public TypeReferenceMetadata(TypeDefinition typeDef)
    {
        AssemblyName = typeDef.Module.Assembly.Name.Name;
        Namespace = typeDef.Namespace;
        Name = typeDef.Name;
    }
}