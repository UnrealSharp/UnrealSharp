using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataStructType(TypeReference structType, string marshallerName, int arrayDim, PropertyType propertyType = PropertyType.Struct) 
    : NativeDataSimpleType(structType, marshallerName, arrayDim, propertyType)
{
    public TypeReferenceMetadata InnerType { get; set; } = new(structType.Resolve());

    public override void PrepareForRewrite(TypeDefinition typeDefinition, FunctionMetaData? functionMetadata,
        PropertyMetaData propertyMetadata)
    {
        base.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);

        if (!WeaverHelper.IsUStruct(InnerType.TypeRef.Resolve()))
        {
            throw new Exception($"{propertyMetadata.MemberRef.FullName} needs to be a UStruct if exposed through UProperty!");
        }
    }
}