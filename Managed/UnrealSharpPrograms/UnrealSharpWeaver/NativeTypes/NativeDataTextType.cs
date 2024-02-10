using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataTextType(TypeReference textType, int arrayDim) : NativeDataType(textType, arrayDim, PropertyType.Text)
{
    private FieldDefinition MarshalerField;
    private MethodReference MarshalerCtor;
    private MethodReference FromNative;

    public override void PrepareForRewrite(TypeDefinition typeDefinition, FunctionMetaData? functionMetadata,
        PropertyMetaData propertyMetadata)
    {
        base.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);

        // Ensure that Text itself is imported.
        WeaverHelper.UserAssembly.MainModule.ImportReference(CSharpType);

        TypeDefinition marshalerType = WeaverHelper.FindTypeInAssembly(WeaverHelper.BindingsAssembly, Program.UnrealSharpNamespace, "TextMarshaller").Resolve();
        MarshalerCtor = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in marshalerType.GetConstructors() 
            where method.Parameters.Count == 1 
                  && method.Parameters[0].ParameterType.FullName == "System.Int32" 
            select method).ToArray()[0]);
        FromNative = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in marshalerType.GetMethods() 
            where method.Name == "FromNative" 
            select method).ToArray()[0]);

        // If this is a rewritten autoproperty, we need an additional backing field for the Text marshaling wrapper.
        // Otherwise, we're copying data for a struct UProp, parameter, or return value.
        string prefix = propertyMetadata.Name + "_";
        PropertyDefinition propertyDef = propertyMetadata.FindPropertyDefinition(typeDefinition);
        
        if (propertyDef == null)
        {
            return;
        }
        
        // Add a field to store the array wrapper for the getter.                
        MarshalerField = new FieldDefinition(prefix + "Wrapper", FieldAttributes.Private, WeaverHelper.UserAssembly.MainModule.ImportReference(marshalerType));
        propertyDef.DeclaringType.Fields.Add(MarshalerField);

        // Suppress the setter.  All modifications should be done by modifying the Text object returned by the getter,
        // which will propagate the changes to the underlying native FText memory.
        propertyDef.DeclaringType.Methods.Remove(propertyDef.SetMethod);
        propertyDef.SetMethod = null;
    }

    public override void EmitFixedArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type)
    {
        throw new NotImplementedException();
    }

    protected override void CreateGetter(TypeDefinition type, MethodDefinition getter, FieldDefinition offsetField,
        FieldDefinition nativePropertyField)
    {
        ILProcessor processor = InitPropertyAccessor(getter);

        /*
          IL_0000:  ldarg.0
          IL_0001:  ldfld      class [UnrealEngine.Runtime]UnrealEngine.Runtime.TextMarshaler UnrealEngine.MonoRuntime.MonoTestsObject::TestReadWriteText_Wrapper
          IL_0006:  brtrue.s   IL_0014
          IL_0008:  ldarg.0
          IL_0009:  ldc.i4.1
          IL_000a:  newobj     instance void [UnrealEngine.Runtime]UnrealEngine.Runtime.TextMarshaler::.ctor(int32)
          IL_000f:  stfld      class [UnrealEngine.Runtime]UnrealEngine.Runtime.TextMarshaler UnrealEngine.MonoRuntime.MonoTestsObject::TestReadWriteText_Wrapper
          IL_0014:  ldarg.0
          IL_0015:  ldfld      class [UnrealEngine.Runtime]UnrealEngine.Runtime.TextMarshaler UnrealEngine.MonoRuntime.MonoTestsObject::TestReadWriteText_Wrapper
          IL_001a:  ldarg.0
          IL_001b:  call       instance native int [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealObject::get_NativeObject()
          IL_0020:  ldsfld     int32 UnrealEngine.MonoRuntime.MonoTestsObject::TestReadWriteText_Offset
          IL_0025:  call       native int [mscorlib]System.IntPtr::op_Addition(native int,
                                                                               int32)
          IL_002a:  ldc.i4.0
          IL_002b:  ldarg.0
          IL_002c:  callvirt   instance class [UnrealEngine.Runtime]UnrealEngine.Runtime.Text [UnrealEngine.Runtime]UnrealEngine.Runtime.TextMarshaler::FromNative(native int,
                                                                                                                                                                   int32,
                                                                                                                                                                   class [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealObject)
         */
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldfld, MarshalerField);

        Instruction branchPosition = processor.Create(OpCodes.Ldarg_0);
        processor.Append(branchPosition);
        processor.Emit(OpCodes.Ldc_I4_1);
        processor.Emit(OpCodes.Newobj, MarshalerCtor);
        processor.Emit(OpCodes.Stfld, MarshalerField);

        Instruction branchTarget = processor.Create(OpCodes.Ldarg_0);
        processor.Append(branchTarget);
        processor.Emit(OpCodes.Ldfld, MarshalerField);
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Call, WeaverHelper.NativeObjectGetter);
        processor.Emit(OpCodes.Ldsfld, offsetField);
        processor.Emit(OpCodes.Call, WeaverHelper.IntPtrAdd);
        processor.Emit(OpCodes.Ldc_I4_0);
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Callvirt, FromNative);

        Instruction branch = processor.Create(OpCodes.Brtrue, branchTarget);
        processor.InsertBefore(branchPosition, branch);

        EndSimpleGetter(processor, getter);

    }

    protected override void CreateSetter(TypeDefinition type, MethodDefinition setter, FieldDefinition offsetField,
        FieldDefinition nativePropertyField)
    {
        throw new NotSupportedException("Text property setters should be stripped, not rewritten.");
    }

    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBufferInstruction, FieldDefinition offsetField, VariableDefinition localVar)
    {
        throw new NotImplementedException();
    }

    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBufferInstruction, FieldDefinition offsetField, FieldDefinition destField)
    {
        throw new NotImplementedException();
    }

    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type,
        Instruction loadBufferInstruction, FieldDefinition offsetField, int argIndex,
        ParameterDefinition paramDefinition)
    {
        throw new NotImplementedException();
    }

    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type, Instruction loadBufferInstruction, FieldDefinition offsetField, FieldDefinition srcField)
    {
        throw new NotImplementedException();
    }

    public override void WriteMarshalFromNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr,
        Instruction loadArrayIndex, Instruction loadOwner)
    {
        throw new NotImplementedException();
    }

    public override void WriteMarshalToNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr,
        Instruction loadArrayIndex, Instruction loadOwner, Instruction[] loadSource)
    {
        throw new NotImplementedException();
    }
}