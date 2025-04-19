using Mono.Cecil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataStructType(TypeReference structType, string marshallerName, int arrayDim, PropertyType propertyType = PropertyType.Struct) 
    : NativeDataSimpleType(structType, marshallerName, arrayDim, propertyType)
{
    public TypeReferenceMetadata InnerType { get; set; } = new(structType.Resolve());

    public override void PrepareForRewrite(TypeDefinition typeDefinition,
        PropertyMetaData propertyMetadata, object outer)
    {
        base.PrepareForRewrite(typeDefinition, propertyMetadata, outer);

        if (!InnerType.TypeRef.Resolve().IsUStruct())
        {
            throw new Exception($"{propertyMetadata.MemberRef!.FullName} needs to be a UStruct if exposed through UProperty!");
        }
    }
}