using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataMulticastDelegate : NativeDataBaseDelegateType
{
    public NativeDataMulticastDelegate(TypeReference delegateType) 
        : base(delegateType, "MulticastDelegateMarshaller`1", PropertyType.MulticastInlineDelegate)
    {
        NeedsNativePropertyField = true;
    }

    public override void PrepareForRewrite(TypeDefinition typeDefinition, PropertyMetaData propertyMetadata,
        object outer)
    {
        AddBackingField(typeDefinition, propertyMetadata);
        base.PrepareForRewrite(typeDefinition, propertyMetadata, outer);
    }
    
    public override void WritePostInitialization(ILProcessor processor, PropertyMetaData propertyMetadata,
        Instruction loadNativePointer, Instruction setNativePointer)
    {
        if (Signature.Parameters.Length == 0)
        {
            return;
        }
        
        TypeReference foundType = GetWrapperType(delegateType);
        MethodReference? initializeDelegateMethod = WeaverHelper.FindMethod(foundType.Resolve(), UnrealDelegateProcessor.InitializeUnrealDelegate);
        processor.Append(loadNativePointer);
        processor.Emit(OpCodes.Call, initializeDelegateMethod);
    }

    public override void WriteGetter(TypeDefinition type, MethodDefinition getter, Instruction[] loadBufferPtr,
        FieldDefinition fieldDefinition)
    {
        ILProcessor processor = BeginSimpleGetter(getter);
        
        foreach (Instruction instruction in loadBufferPtr)
        {
            processor.Append(instruction);
        }
        
        processor.Emit(OpCodes.Ldsfld, fieldDefinition);
        processor.Emit(OpCodes.Ldc_I4_0);

        processor.Emit(OpCodes.Call, FromNative);
        getter.FinalizeMethod();
    }
}