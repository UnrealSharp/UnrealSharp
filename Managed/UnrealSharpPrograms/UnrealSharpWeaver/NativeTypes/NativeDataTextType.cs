using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataTextType(TypeReference textType, int arrayDim) : NativeDataType(textType, arrayDim, PropertyType.Text)
{
    private FieldDefinition MarshallerField;
    private MethodReference MarshallerConstructor;
    private MethodReference FromNative;

    public override void PrepareForRewrite(TypeDefinition typeDefinition, FunctionMetaData? functionMetadata,
        PropertyMetaData propertyMetadata)
    {
        base.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);

        // Ensure that Text itself is imported.
        WeaverHelper.UserAssembly.MainModule.ImportReference(CSharpType);

        TypeDefinition marshallerType = WeaverHelper.FindTypeInAssembly(WeaverHelper.BindingsAssembly, Program.UnrealSharpNamespace, "TextMarshaller").Resolve();
        MarshallerConstructor = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in marshallerType.GetConstructors() 
            where method.Parameters.Count == 1 
                  && method.Parameters[0].ParameterType.FullName == "System.Int32" 
            select method).ToArray()[0]);
        FromNative = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in marshallerType.GetMethods() 
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
        MarshallerField = new FieldDefinition(prefix + "Wrapper", FieldAttributes.Private, WeaverHelper.UserAssembly.MainModule.ImportReference(marshallerType));
        propertyDef.DeclaringType.Fields.Add(MarshallerField);

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
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldfld, MarshallerField);

        Instruction branchPosition = processor.Create(OpCodes.Ldarg_0);
        processor.Append(branchPosition);
        processor.Emit(OpCodes.Ldc_I4_1);
        processor.Emit(OpCodes.Newobj, MarshallerConstructor);
        processor.Emit(OpCodes.Stfld, MarshallerField);

        Instruction branchTarget = processor.Create(OpCodes.Ldarg_0);
        processor.Append(branchTarget);
        processor.Emit(OpCodes.Ldfld, MarshallerField);
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