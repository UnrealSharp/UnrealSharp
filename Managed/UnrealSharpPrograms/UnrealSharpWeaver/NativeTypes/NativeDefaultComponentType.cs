using Mono.Cecil;
using Mono.Collections.Generic;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataDefaultComponent : NativeDataSimpleType
{
    public NativeDataDefaultComponent(Collection<CustomAttribute> customAttributes, TypeReference typeRef, string marshallerName, int arrayDim) 
        : base(typeRef, marshallerName, arrayDim, PropertyType.DefaultComponent)
    {
        var upropertyAttribute = WeaverHelper.FindAttributeByType(customAttributes, Program.AttributeNamespace, "UPropertyAttribute");
        
        CustomAttributeArgument? isRootComponentValue = WeaverHelper.FindAttributeField(upropertyAttribute, "RootComponent");
        CustomAttributeArgument? AttachmentComponentValue = WeaverHelper.FindAttributeField(upropertyAttribute, "AttachmentComponent");
        CustomAttributeArgument? AttachmentSocketValue = WeaverHelper.FindAttributeField(upropertyAttribute, "AttachmentSocket");

        if (isRootComponentValue != null)
        {
            IsRootComponent = (bool) isRootComponentValue.Value.Value;
        }

        if (AttachmentComponentValue != null)
        {
            AttachmentComponent = (string) AttachmentComponentValue.Value.Value;
        }
        
        if (AttachmentSocketValue != null)
        {
            AttachmentSocket = (string) AttachmentSocketValue.Value.Value;
        }

        InnerType = new TypeReferenceMetadata(typeRef.Resolve());
    }
    
    public bool IsRootComponent { get; set; }
    public string AttachmentComponent { get; set; }
    public string AttachmentSocket { get; set; }
    public TypeReferenceMetadata InnerType { get; set; }

    public static bool IsDefaultComponent(Collection<CustomAttribute>? customAttributes)
    {
        if (customAttributes == null)
        {
            return false;
        }
        
        var upropertyAttribute = WeaverHelper.FindAttributeByType(customAttributes, Program.AttributeNamespace, "UPropertyAttribute");

        if (upropertyAttribute == null)
        {
            return false;
        }
        
        CustomAttributeArgument? isDefaultComponent = WeaverHelper.FindAttributeField(upropertyAttribute, "DefaultComponent");

        if (isDefaultComponent == null)
        {
            return false;
        }

        bool isDefaultComponentValue = (bool)isDefaultComponent.Value.Value;
        return isDefaultComponentValue;
    }
}