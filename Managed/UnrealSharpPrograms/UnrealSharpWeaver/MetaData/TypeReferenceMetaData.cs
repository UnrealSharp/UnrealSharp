using Mono.Cecil;

namespace UnrealSharpWeaver.MetaData;

public class TypeReferenceMetadata(TypeReference member, string attributeName = "") : BaseMetaData(member, attributeName)
{
    public string AssemblyName { get; set; } = member.Module.Assembly.Name.Name;
    public string Namespace { get; set; } = member.Namespace;

    // Non-serialized for JSON
    public readonly TypeReference TypeDef = member;
    // End non-serialized
}