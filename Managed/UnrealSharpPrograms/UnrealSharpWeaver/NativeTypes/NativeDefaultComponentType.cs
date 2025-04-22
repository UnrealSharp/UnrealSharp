using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataDefaultComponent : NativeDataSimpleType
{
    public bool IsRootComponent { get; set; }
    public string AttachmentComponent { get; set; } = string.Empty;
    public string AttachmentSocket { get; set; } = string.Empty;
    public TypeReferenceMetadata InnerType { get; set; }
    
    public NativeDataDefaultComponent(Collection<CustomAttribute> customAttributes, TypeReference typeRef, int arrayDim) 
        : base(typeRef, "DefaultComponentMarshaller`1", arrayDim, PropertyType.DefaultComponent)
    {
        TypeDefinition? defaultComponentType = typeRef.Resolve();
        
        if (!defaultComponentType.IsUObject())
        {
            throw new Exception($"{defaultComponentType.FullName} needs to be a UClass if exposed through UProperty!");
        }
        
        InnerType = new TypeReferenceMetadata(defaultComponentType);
        
        CustomAttribute upropertyAttribute = PropertyUtilities.GetUProperty(customAttributes)!;
        
        CustomAttributeArgument? isRootComponentValue = upropertyAttribute.FindAttributeField("RootComponent");
        if (isRootComponentValue != null)
        {
            IsRootComponent = (bool) isRootComponentValue.Value.Value;
        }

        CustomAttributeArgument? attachmentComponentValue = upropertyAttribute.FindAttributeField("AttachmentComponent");
        if (attachmentComponentValue != null)
        {
            AttachmentComponent = (string) attachmentComponentValue.Value.Value;
        }
        
        CustomAttributeArgument? attachmentSocketValue = upropertyAttribute.FindAttributeField("AttachmentSocket");
        if (attachmentSocketValue != null)
        {
            AttachmentSocket = (string) attachmentSocketValue.Value.Value;
        }
    }

    public override void WriteGetter(TypeDefinition type, MethodDefinition getter, Instruction[] loadBufferPtr,
        FieldDefinition? fieldDefinition)
    {
        ILProcessor processor = BeginSimpleGetter(getter);
        string propertyName = getter.Name.Substring(4);
        
        List<Instruction> loadBuffer = new List<Instruction>();
        loadBuffer.Add(processor.Create(OpCodes.Ldarg_0));
        loadBuffer.Add(processor.Create(OpCodes.Ldstr, propertyName));
        
        foreach (Instruction instruction in loadBufferPtr)
        {
            loadBuffer.Add(instruction);
        }
        
        WriteMarshalFromNative(processor, type, loadBuffer.ToArray(), processor.Create(OpCodes.Ldc_I4_0));
        getter.FinalizeMethod();
    }

    public static bool IsDefaultComponent(Collection<CustomAttribute>? customAttributes)
    {
        if (customAttributes == null)
        {
            return false;
        }
        
        var upropertyAttribute = PropertyUtilities.GetUProperty(customAttributes);

        if (upropertyAttribute == null)
        {
            return false;
        }
        
        CustomAttributeArgument? isDefaultComponent = upropertyAttribute.FindAttributeField("DefaultComponent");

        if (isDefaultComponent == null)
        {
            return false;
        }

        bool isDefaultComponentValue = (bool)isDefaultComponent.Value.Value;
        return isDefaultComponentValue;
    }
}