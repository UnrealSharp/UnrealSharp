using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Rewriters;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataMulticastDelegate : NativeDataBaseDelegateType
{
    public NativeDataMulticastDelegate(TypeReference delegateType) 
        : base(delegateType, "DelegateMarshaller`1", PropertyType.MulticastInlineDelegate)
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
        if (Signature?.Parameters.Length == 0)
        {
            return;
        }
        
        PropertyDefinition propertyRef = (PropertyDefinition) propertyMetadata.MemberRef.Resolve();
        MethodReference? Initialize = WeaverHelper.FindMethod(propertyRef.PropertyType.Resolve(), UnrealDelegateProcessor.InitializeUnrealDelegate);
        processor.Append(loadNativePointer);
        processor.Emit(OpCodes.Call, Initialize);
    }

    protected override void CreateGetter(TypeDefinition type, MethodDefinition getter, FieldDefinition offsetField, FieldDefinition nativePropertyField)
    {
        ILProcessor processor = BeginSimpleGetter(getter);
        Instruction loadOwner = processor.Create(OpCodes.Ldarg_0);
        
        //processor.Emit(OpCodes.Ldarg_0);
        //processor.Emit(OpCodes.Ldfld, BackingField);
        //var ifEnd = Instruction.Create(OpCodes.Nop);
        //processor.Emit(OpCodes.Brtrue, ifEnd);
        
        // Push the native property field onto the stack
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, null, offsetField);
        
        foreach (var i in loadBufferInstructions)
        {
            processor.Append(i);
        }
        
        // Push the native property field onto the stack
        processor.Emit(OpCodes.Ldsfld, nativePropertyField);
        
        // Push 0 onto the stack
        processor.Emit(OpCodes.Ldc_I4_0);
        
        // Push this onto the stack
        processor.Append(loadOwner);

        processor.Emit(OpCodes.Call, FromNative);
        EndSimpleGetter(processor, getter);
    }
}