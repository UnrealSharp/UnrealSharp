using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataEnumType(TypeReference typeRef, int arrayDim) : NativeDataSimpleType(typeRef, "EnumMarshaller`1", arrayDim, PropertyType.Enum)
{
    public TypeReferenceMetadata InnerProperty { get; set; } = new(typeRef.Resolve());

    public override void PrepareForRewrite(TypeDefinition typeDefinition,
        PropertyMetaData propertyMetadata, object outer)
    {
        base.PrepareForRewrite(typeDefinition, propertyMetadata, outer);
        
        if (!WeaverHelper.IsUEnum(InnerProperty.TypeRef.Resolve()))
        {
            throw new Exception($"{propertyMetadata.MemberRef.FullName} needs to be a UEnum if exposed through UProperty!");
        }
    }
};