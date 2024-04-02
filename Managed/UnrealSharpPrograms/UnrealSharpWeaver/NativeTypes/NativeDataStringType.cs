using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataStringType : NativeDataType
{
    private MethodReference ToNative;
    private MethodReference FromNative;
    private MethodReference ToNativeWithCleanup;
    private MethodReference DestructInstance;

    public NativeDataStringType(TypeReference typeRef, int arrayDim) : base(typeRef, arrayDim, PropertyType.Str)
    {
        NeedsNativePropertyField = true;
    }

    public override void PrepareForRewrite(TypeDefinition typeDefinition, FunctionMetaData? functionMetadata, PropertyMetaData propertyMetadata)
    {
        base.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);

        string marshallerNamespace = Program.UnrealSharpNamespace;
        AssemblyDefinition marshallerAssembly = WeaverHelper.BindingsAssembly;

        TypeDefinition marshallerType = WeaverHelper.FindTypeInAssembly(marshallerAssembly, marshallerNamespace, "StringMarshaller").Resolve();
        TypeDefinition marshallerTypeWithCleanup = WeaverHelper.FindTypeInAssembly(marshallerAssembly, marshallerNamespace, "StringMarshaller").Resolve();

        ToNative = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in marshallerType.GetMethods() where method.IsStatic && method.Name == "ToNative" select method).ToArray()[0]);
        FromNative = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in marshallerType.GetMethods() where method.IsStatic && method.Name == "FromNative" select method).ToArray()[0]);
        ToNativeWithCleanup = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in marshallerTypeWithCleanup.GetMethods() where method.IsStatic && method.Name == "ToNative" select method).ToArray()[0]);
        DestructInstance = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in marshallerTypeWithCleanup.GetMethods() where method.IsStatic && method.Name == "DestructInstance" select method).ToArray()[0]);
    }

    public override void EmitFixedArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type)
    {
        EmitSimpleMarshallerDelegates(processor, "StringMarshaller", null);
    }

    protected override void CreateGetter(TypeDefinition type, MethodDefinition getter, FieldDefinition offsetField, FieldDefinition nativePropertyField)
    {
        ILProcessor processor = BeginSimpleGetter(getter);
        Instruction loadOwner = processor.Create(OpCodes.Ldarg_0);
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, null, offsetField);
        WriteMarshalFromNative(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), loadOwner);
        EndSimpleGetter(processor, getter);
    }
    
    protected override void CreateSetter(TypeDefinition type, MethodDefinition setter, FieldDefinition offsetField, FieldDefinition nativePropertyField)
    {
        ILProcessor processor = BeginSimpleSetter(setter);
        Instruction loadValue = processor.Create(OpCodes.Ldarg_1);
        Instruction loadOwner = processor.Create(OpCodes.Ldarg_0);
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, null, offsetField);
        WriteMarshalToNative(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), loadOwner, loadValue);
        EndSimpleSetter(processor, setter);
    }

    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBuffer,
        FieldDefinition offsetField, VariableDefinition localVar)
    {
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBuffer, offsetField);
        WriteMarshalFromNative(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), processor.Create(OpCodes.Ldnull));
        processor.Emit(OpCodes.Stloc, localVar);
    }
    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBuffer,
        FieldDefinition offsetField, FieldDefinition destField)
    {
        processor.Emit(OpCodes.Ldarg_0);
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBuffer, offsetField);
        WriteMarshalFromNative(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), processor.Create(OpCodes.Ldnull));
        processor.Emit(OpCodes.Stfld, destField);
    }

    public override void WriteMarshalToNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr,
        Instruction loadArrayIndex, Instruction loadOwner, Instruction[] loadSourceInstructions)
    {
        foreach (var i in loadBufferPtr)
        {
            processor.Append(i);
        }
        processor.Append(loadArrayIndex);
        processor.Append(loadOwner);
        foreach (Instruction i in loadSourceInstructions) //source
        {
            processor.Append(i);
        }
        processor.Emit(OpCodes.Call, ToNative);
    }

    public override void WriteMarshalFromNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr,
        Instruction loadArrayIndex, Instruction loadOwner)
    {
        foreach (var i in loadBufferPtr)
        {
            processor.Append(i);
        }
        processor.Append(loadArrayIndex);
        processor.Append(loadOwner);
        processor.Emit(OpCodes.Call, FromNative);
    }

    public override IList<Instruction>? WriteMarshalToNativeWithCleanup(ILProcessor processor, TypeDefinition type,
        Instruction[] loadBufferPtr, Instruction loadArrayIndex, Instruction loadOwner,
        Instruction[] loadSourceInstructions)
    {
        foreach (var i in loadBufferPtr)
        {
            processor.Append(i);
        }
        processor.Append(loadArrayIndex);
        processor.Append(loadOwner);
        foreach (Instruction i in loadSourceInstructions) //source
        {
            processor.Append(i);
        }
        processor.Emit(OpCodes.Call, ToNativeWithCleanup);

        IList<Instruction> cleanupInstructions = new List<Instruction>();
        foreach (var i in loadBufferPtr)
        {
            cleanupInstructions.Add(i);
        }
        cleanupInstructions.Add(loadArrayIndex);
        cleanupInstructions.Add(processor.Create(OpCodes.Call, DestructInstance));
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
            _ => [processor.Create(OpCodes.Ldarg_S, argIndex)],
        };
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBuffer, offsetField);
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), processor.Create(OpCodes.Ldnull), loadSource);
    }
    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type, Instruction loadBuffer,
        FieldDefinition offsetField, FieldDefinition srcField)
    {
        Instruction[] loadSource = 
        {
            processor.Create(OpCodes.Ldarg_0),
            processor.Create(OpCodes.Ldfld, srcField),
        };
        
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBuffer, offsetField);
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), processor.Create(OpCodes.Ldnull), loadSource);
    }
}