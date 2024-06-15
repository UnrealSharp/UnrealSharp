using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataStringType(TypeReference typeRef, int arrayDim) : NativeDataType(typeRef, arrayDim, PropertyType.String)
{
    private static MethodReference _toNative;
    private static MethodReference _fromNative;
    private static MethodReference _destructInstance;
    private static bool _bInitialized;

    public override void PrepareForRewrite(TypeDefinition typeDefinition, FunctionMetaData? functionMetadata, PropertyMetaData propertyMetadata)
    {
        base.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);
        
        if (_bInitialized)
        {
            return;
        }
        
        TypeDefinition marshallerType = WeaverHelper.FindTypeInAssembly(WeaverHelper.BindingsAssembly, "StringMarshaller", WeaverHelper.UnrealSharpNamespace)!.Resolve();
        _toNative = WeaverHelper.FindMethod(marshallerType, "ToNative")!;
        _fromNative = WeaverHelper.FindMethod(marshallerType, "FromNative")!;
        _destructInstance = WeaverHelper.FindMethod(marshallerType, "DestructInstance")!;
        _bInitialized = true;
    }

    public override void EmitFixedArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type)
    {
        EmitSimpleMarshallerDelegates(processor, "StringMarshaller", null);
    }

    protected override void CreateGetter(TypeDefinition type, MethodDefinition getter, FieldDefinition offsetField, FieldDefinition nativePropertyField)
    {
        ILProcessor processor = BeginSimpleGetter(getter);
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, null, offsetField);
        WriteMarshalFromNative(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0));
        EndSimpleGetter(processor, getter);
    }
    
    protected override void CreateSetter(TypeDefinition type, MethodDefinition setter, FieldDefinition offsetField, FieldDefinition nativePropertyField)
    {
        ILProcessor processor = BeginSimpleSetter(setter);
        Instruction loadValue = processor.Create(OpCodes.Ldarg_1);
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, null, offsetField);
        WriteMarshalToNative(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), loadValue);
        EndSimpleSetter(processor, setter);
    }

    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBuffer, FieldDefinition offsetField, VariableDefinition localVar)
    {
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBuffer, offsetField);
        WriteMarshalFromNative(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0));
        processor.Emit(OpCodes.Stloc, localVar);
    }
    
    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBuffer, FieldDefinition offsetField, FieldDefinition destField)
    {
        processor.Emit(OpCodes.Ldarg_0);
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBuffer, offsetField);
        WriteMarshalFromNative(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0));
        processor.Emit(OpCodes.Stfld, destField);
    }

    public override void WriteMarshalToNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr, Instruction loadArrayIndex, Instruction[] loadSourceInstructions)
    {
        foreach (var i in loadBufferPtr)
        {
            processor.Append(i);
        }
        processor.Append(loadArrayIndex);
        foreach (Instruction i in loadSourceInstructions)
        {
            processor.Append(i);
        }
        processor.Emit(OpCodes.Call, _toNative);
    }

    public override void WriteMarshalFromNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr, Instruction loadArrayIndex)
    {
        foreach (var i in loadBufferPtr)
        {
            processor.Append(i);
        }
        processor.Append(loadArrayIndex);
        processor.Emit(OpCodes.Call, _fromNative);
    }

    public override IList<Instruction>? WriteMarshalToNativeWithCleanup(ILProcessor processor, TypeDefinition type,
        Instruction[] loadBufferPtr, Instruction loadArrayIndex,
        Instruction[] loadSourceInstructions)
    {
        WriteMarshalToNative(processor, type, loadBufferPtr, loadArrayIndex, loadSourceInstructions);

        Instruction offsteField = loadBufferPtr[1];
        IList<Instruction> cleanupInstructions = new List<Instruction>(); ;
        cleanupInstructions.Add(Instruction.Create(OpCodes.Ldloc_1));
        cleanupInstructions.Add(offsteField);
        cleanupInstructions.Add(Instruction.Create(OpCodes.Call, WeaverHelper.IntPtrAdd));
        cleanupInstructions.Add(loadArrayIndex);
        cleanupInstructions.Add(processor.Create(OpCodes.Call, _destructInstance));
        
        return cleanupInstructions;
    }

    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type, Instruction loadBuffer,
        FieldDefinition offsetField, int argIndex, ParameterDefinition paramDefinition)
    {
        Instruction[] loadSource = argIndex switch
        {
            0 => [processor.Create(OpCodes.Ldarg_0)],
            1 => [processor.Create(OpCodes.Ldarg_1)],
            2 => [processor.Create(OpCodes.Ldarg_2)],
            3 => [processor.Create(OpCodes.Ldarg_3)],
            _ => [processor.Create(OpCodes.Ldarg_S, (byte)argIndex)],
        };
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBuffer, offsetField);
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), loadSource);
    }
    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type, Instruction loadBuffer, FieldDefinition offsetField, FieldDefinition srcField)
    {
        Instruction[] loadSource = 
        {
            processor.Create(OpCodes.Ldarg_0),
            processor.Create(OpCodes.Ldfld, srcField),
        };
        
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBuffer, offsetField);
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), loadSource);
    }
}