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

    public override void PrepareForRewrite(TypeDefinition typeDefinition, FunctionMetaData? functionMetadata, PropertyMetaData propertyMetadata)
    {
        AddBackingField(typeDefinition, propertyMetadata);
        base.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);
    }
    
    public override void WritePostInitialization(ILProcessor processor, PropertyMetaData propertyMetadata,
        Instruction loadNativePointer, Instruction setNativePointer)
    {
        if (Signature.Parameters.Length == 0)
        {
            return;
        }
        
        TypeReference wrapperType = WeaverHelper.FindTypeInAssembly(delegateType.Module.Assembly, $"U{delegateType.Name}", delegateType.Namespace)!;
        MethodReference? initializeDelegateMethod = WeaverHelper.FindMethod(wrapperType.Resolve(), UnrealDelegateProcessor.InitializeUnrealDelegate);
        processor.Append(loadNativePointer);
        processor.Emit(OpCodes.Call, initializeDelegateMethod);
    }

    protected override void CreateGetter(TypeDefinition type, MethodDefinition getter, FieldDefinition offsetField, FieldDefinition nativePropertyField)
    {
        ILProcessor processor = BeginSimpleGetter(getter);
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, null, offsetField);
        
        foreach (var i in loadBufferInstructions)
        {
            processor.Append(i);
        }
        
        processor.Emit(OpCodes.Ldsfld, nativePropertyField);
        processor.Emit(OpCodes.Ldc_I4_0);

        processor.Emit(OpCodes.Call, FromNative);
        EndSimpleGetter(processor, getter);
    }
}