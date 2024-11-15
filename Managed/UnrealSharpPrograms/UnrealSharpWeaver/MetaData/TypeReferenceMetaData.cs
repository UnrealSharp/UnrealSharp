using Mono.Cecil;

namespace UnrealSharpWeaver.MetaData;

public class TypeReferenceMetadata : BaseMetaData
{
    public string AssemblyName { get; set; }
    public string Namespace { get; set; }
    
    // Non-serialized for JSON
    public readonly TypeReference TypeRef;
    // End non-serialized
    
    public TypeReferenceMetadata(TypeReference member, string attributeName = "") : base(member, attributeName)
    {
        AssemblyName = member.Module.Assembly.Name.Name;
        Namespace = member.Namespace;
        TypeRef = member;
    }
}