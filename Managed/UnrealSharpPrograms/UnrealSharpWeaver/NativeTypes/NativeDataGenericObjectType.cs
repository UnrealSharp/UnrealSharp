using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

abstract class NativeDataGenericObjectType(TypeReference typeRef, TypeReference innerTypeReference, string marshallerClass, int arrayDim, PropertyType propertyType)
    : NativeDataSimpleType(typeRef, marshallerClass, arrayDim, propertyType)
{
    public TypeReferenceMetadata InnerType { get; set; } = new(innerTypeReference.Resolve());

    public override void PrepareForRewrite(TypeDefinition typeDefinition, FunctionMetaData? functionMetadata,
        PropertyMetaData propertyMetadata)
    {
        base.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);
        
        if (!WeaverHelper.IsValidBaseForUObject(InnerType.TypeRef.Resolve()))
        {
            throw new Exception($"{propertyMetadata.MemberRef.FullName} needs to be a UClass if exposed through UProperty!");
        }
    }
}