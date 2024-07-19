using Mono.Cecil;
using Mono.Collections.Generic;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataDefaultComponent : NativeDataSimpleType
{
    public NativeDataDefaultComponent(Collection<CustomAttribute> customAttributes, TypeReference typeRef, string marshallerName, int arrayDim) 
        : base(typeRef, marshallerName, arrayDim, PropertyType.DefaultComponent)
    {
        var defaultComponentType = typeRef.Resolve();
        
        if (!WeaverHelper.IsValidBaseForUObject(defaultComponentType))
        {
            throw new Exception($"{defaultComponentType.FullName} needs to be a UClass if exposed through UProperty!");
        }
        
        InnerType = new TypeReferenceMetadata(defaultComponentType);
        
        CustomAttribute upropertyAttribute = WeaverHelper.GetUProperty(customAttributes)!;
        
        CustomAttributeArgument? isRootComponentValue = WeaverHelper.FindAttributeField(upropertyAttribute, "RootComponent");
        if (isRootComponentValue != null)
        {
            IsRootComponent = (bool) isRootComponentValue.Value.Value;
        }

        CustomAttributeArgument? attachmentComponentValue = WeaverHelper.FindAttributeField(upropertyAttribute, "AttachmentComponent");
        if (attachmentComponentValue != null)
        {
            AttachmentComponent = (string) attachmentComponentValue.Value.Value;
        }
        
        CustomAttributeArgument? attachmentSocketValue = WeaverHelper.FindAttributeField(upropertyAttribute, "AttachmentSocket");
        if (attachmentSocketValue != null)
        {
            AttachmentSocket = (string) attachmentSocketValue.Value.Value;
        }
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
        
        var upropertyAttribute = WeaverHelper.GetUProperty(customAttributes);

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