using Mono.Cecil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.NativeTypes;

abstract class NativeDataGenericObjectType(TypeReference typeRef, TypeReference innerTypeReference, string marshallerClass, int arrayDim, PropertyType propertyType)
    : NativeDataSimpleType(typeRef, marshallerClass, arrayDim, propertyType)
{
    public TypeReferenceMetadata InnerType { get; set; } = new(innerTypeReference.Resolve());

    public override void PrepareForRewrite(TypeDefinition typeDefinition, PropertyMetaData propertyMetadata,
        object outer)
    {
        base.PrepareForRewrite(typeDefinition, propertyMetadata, outer);
        
        if (!InnerType.TypeRef.Resolve().IsUObject())
        {
            throw new Exception($"{propertyMetadata.MemberRef!.FullName} needs to be a UClass if exposed through UProperty!");
        }
    }
}