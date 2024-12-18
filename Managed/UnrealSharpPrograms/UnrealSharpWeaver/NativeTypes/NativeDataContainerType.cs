using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataContainerType : NativeDataType
{
    public PropertyMetaData InnerProperty { get; set; }
    
    protected TypeReference ContainerMarshallerType;
    protected MethodReference ContainerMashallerCtor;
    protected FieldDefinition ContainerMarshallerField;
    protected TypeReference[] ContainerMarshallerTypeParameters;
    
    protected MethodReference FromNative;
    protected MethodReference ToNative;
    
    protected FieldDefinition NativePropertyField;
    
    protected MethodReference? CopyDestructInstance;
    
    public NativeDataContainerType(TypeReference typeRef, int containerDim, PropertyType propertyType, TypeReference value) : base(typeRef, containerDim, propertyType)
    {
        InnerProperty = PropertyMetaData.FromTypeReference(value, "Inner");
        NeedsNativePropertyField = true;
    }

    public virtual string GetContainerMarshallerName()
    {
        throw new NotImplementedException();
    }
    
    public virtual string GetCopyContainerMarshallerName()
    {
        throw new NotImplementedException();
    }
    
    public virtual void InitializeMarshallerParameters()
    {
        ContainerMarshallerTypeParameters = [WeaverHelper.ImportType(InnerProperty.PropertyDataType.CSharpType)];
    }
    
    public virtual string GetContainerWrapperType()
    {
        throw new NotImplementedException();
    }
    
    public override void EmitFixedArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type)
    {
        throw new NotImplementedException();
    }
    
    public override void EmitDynamicArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type)
    {
        InnerProperty.PropertyDataType.EmitDynamicArrayMarshallerDelegates(processor, type);
    }

    public override void PrepareForRewrite(TypeDefinition typeDefinition, PropertyMetaData propertyMetadata, object outer)
    {
        base.PrepareForRewrite(typeDefinition, propertyMetadata, outer);
        InnerProperty.PropertyDataType.PrepareForRewrite(typeDefinition, propertyMetadata, "");

        // Ensure that IList<T> itself is imported.
        WeaverHelper.ImportType(CSharpType);

        InitializeMarshallerParameters();

        // Instantiate generics for the direct access and copying marshallers.
        string prefix = propertyMetadata.Name + "_";
        
        FieldAttributes fieldAttributes = FieldAttributes.Private;
        if (outer is MethodDefinition method)
        {
            prefix = method.Name + "_" + prefix;
            TypeReference genericCopyMarshallerTypeRef = WeaverHelper.FindTypeInAssembly(WeaverHelper.BindingsAssembly, 
                GetCopyContainerMarshallerName(), WeaverHelper.UnrealSharpNamespace)!;
            
            ContainerMarshallerType = WeaverHelper.ImportType(genericCopyMarshallerTypeRef.Resolve().MakeGenericInstanceType(ContainerMarshallerTypeParameters));
            
            CopyDestructInstance = WeaverHelper.FindMethod(ContainerMarshallerType.Resolve(), "DestructInstance")!;
            CopyDestructInstance = FunctionProcessor.MakeMethodDeclaringTypeGeneric(CopyDestructInstance, ContainerMarshallerTypeParameters);
            
            fieldAttributes |= FieldAttributes.Static;
        }
        else
        {
            TypeReference genericCopyMarshallerTypeRef = WeaverHelper.FindTypeInAssembly(WeaverHelper.BindingsAssembly, 
                GetContainerMarshallerName(), WeaverHelper.UnrealSharpNamespace)!;
            
            ContainerMarshallerType = WeaverHelper.ImportType(genericCopyMarshallerTypeRef.Resolve().MakeGenericInstanceType(ContainerMarshallerTypeParameters));

            if (propertyMetadata.MemberRef is PropertyDefinition propertyDefinition)
            {
                typeDefinition.Methods.Remove(propertyDefinition.SetMethod);
                propertyDefinition.SetMethod = null;
            }
        }
        
        TypeDefinition arrTypeDef = ContainerMarshallerType.Resolve();
        
        ContainerMashallerCtor = arrTypeDef.GetConstructors().Single();
        ContainerMashallerCtor = WeaverHelper.ImportMethod(ContainerMashallerCtor);
        ContainerMashallerCtor = FunctionProcessor.MakeMethodDeclaringTypeGeneric(ContainerMashallerCtor, ContainerMarshallerTypeParameters);
        
        FromNative = WeaverHelper.FindMethod(arrTypeDef, "FromNative")!;
        FromNative = FunctionProcessor.MakeMethodDeclaringTypeGeneric(FromNative, ContainerMarshallerTypeParameters);

        ToNative = WeaverHelper.FindMethod(arrTypeDef, "ToNative")!;
        ToNative = FunctionProcessor.MakeMethodDeclaringTypeGeneric(ToNative, ContainerMarshallerTypeParameters);
        
        ContainerMarshallerField = WeaverHelper.AddFieldToType(typeDefinition, prefix + "Marshaller", ContainerMarshallerType, fieldAttributes);
        NativePropertyField = propertyMetadata.NativePropertyField!;
    }

    public override void WriteGetter(TypeDefinition type, MethodDefinition getter, Instruction[] loadBufferPtr, FieldDefinition fieldDefinition)
    {
        ILProcessor processor = InitPropertyAccessor(getter);

        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldfld, ContainerMarshallerField);

        // Save the position of the branch instruction for later, when we have a reference to its target.
        processor.Emit(OpCodes.Ldarg_0);
        Instruction branchPosition = processor.Body.Instructions[^1];
        
        processor.Emit(OpCodes.Ldsfld, fieldDefinition);
        EmitDynamicArrayMarshallerDelegates(processor, type);

        MethodDefinition? constructor = ContainerMarshallerType.Resolve().GetConstructors().Single();
        processor.Emit(OpCodes.Newobj, FunctionProcessor.MakeMethodDeclaringTypeGeneric(WeaverHelper.UserAssembly.MainModule.ImportReference(constructor), ContainerMarshallerTypeParameters));
        processor.Emit(OpCodes.Stfld, ContainerMarshallerField);

        // Store the branch destination
        processor.Emit(OpCodes.Ldarg_0);
        Instruction branchTarget = processor.Body.Instructions[^1];
        processor.Emit(OpCodes.Ldfld, ContainerMarshallerField);
        
        foreach (Instruction inst in loadBufferPtr)
        {
            processor.Append(inst);
        }
        
        processor.Emit(OpCodes.Ldc_I4_0);
        processor.Emit(OpCodes.Call, FromNative);
        
        //Now insert the branch
        Instruction branchInstruction = processor.Create(OpCodes.Brtrue_S, branchTarget);
        processor.InsertBefore(branchPosition, branchInstruction);

        getter.FinalizeMethod();
    }

    public override void WriteSetter(TypeDefinition type, MethodDefinition setter, Instruction[] loadBufferPtr,
        FieldDefinition fieldDefinition)
    {
        
    }

    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBufferInstruction,
        FieldDefinition offsetField, VariableDefinition localVar)
    {
        WriteMarshalFromNative(processor, type, GetArgumentBufferInstructions(loadBufferInstruction, offsetField), processor.Create(OpCodes.Ldc_I4_0));
        processor.Emit(OpCodes.Stloc, localVar);
    }
    
    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBufferInstruction,
        FieldDefinition offsetField, FieldDefinition destField)
    {
        processor.Emit(OpCodes.Ldarg_0);
        WriteMarshalFromNative(processor, type, GetArgumentBufferInstructions(loadBufferInstruction, offsetField), processor.Create(OpCodes.Ldc_I4_0));
        processor.Emit(OpCodes.Stfld, destField);
    }

    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type,
        Instruction loadBufferInstruction, FieldDefinition offsetField, int argIndex,
        ParameterDefinition paramDefinition)
    {
        Instruction[] loadSource = [processor.Create(OpCodes.Ldarg, argIndex)];
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(loadBufferInstruction, offsetField);
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), loadSource);
    }
    
    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type,
        Instruction loadBufferInstruction, FieldDefinition offsetField, FieldDefinition srcField)
    {
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(loadBufferInstruction, offsetField);
        Instruction[] loadSource = [processor.Create(OpCodes.Ldarg_0), processor.Create(OpCodes.Ldfld, srcField)];
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), loadSource);
    }

    public override void WriteMarshalFromNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr, Instruction loadContainerIndex)
    {
        WriteInitMarshaller(processor, type);
        AppendLoadMarshallerInstructions(processor);
        
        foreach (Instruction inst in loadBufferPtr)
        {
            processor.Append(inst);
        }
        
        processor.Emit(OpCodes.Ldc_I4_0);
        processor.Emit(OpCodes.Call, FromNative);
    }

    public override void WriteMarshalToNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr,
        Instruction loadContainerIndex, Instruction[] loadSource)
    {
        WriteInitMarshaller(processor, type);
        AppendLoadMarshallerInstructions(processor);
        
        foreach (var i in loadBufferPtr)
        {
            processor.Append(i);
        }
        
        processor.Append(loadContainerIndex);
        
        foreach( var i in loadSource)
        {
            processor.Append(i);
        }
        
        processor.Emit(OpCodes.Call, ToNative);
    }

    public override IList<Instruction>? WriteMarshalToNativeWithCleanup(ILProcessor processor, TypeDefinition type,
        Instruction[] loadBufferPtr, Instruction loadContainerIndex, Instruction[] loadSource)
    {
        WriteMarshalToNative(processor, type, loadBufferPtr, loadContainerIndex, loadSource);
        return null;
    }

    private void WriteInitMarshaller(ILProcessor processor, TypeDefinition type)
    {
        AppendLoadMarshallerInstructions(processor);
        
        Instruction branchTarget = processor.Create(OpCodes.Nop);
        processor.Emit(OpCodes.Brtrue_S, branchTarget);
        
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldsfld, NativePropertyField);
        
        EmitDynamicArrayMarshallerDelegates(processor, type);
        
        processor.Emit(OpCodes.Newobj, ContainerMashallerCtor);
        processor.Emit(OpCodes.Stfld, ContainerMarshallerField);
        
        processor.Append(branchTarget);
    }
    
    private void AppendLoadMarshallerInstructions(ILProcessor processor)
    {
        if (ContainerMarshallerField.IsStatic)
        {  
            processor.Emit(OpCodes.Ldsfld, ContainerMarshallerField);
        }
        else
        {
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldfld, ContainerMarshallerField);
        }

    }
}
