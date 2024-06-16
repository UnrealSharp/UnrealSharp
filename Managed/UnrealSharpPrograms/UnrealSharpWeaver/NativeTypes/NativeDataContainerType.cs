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
    protected TypeReference CopyContainerMarshallerType;
    protected FieldDefinition ContainerMarshallerField;
    protected TypeReference[] ContainerMarshallerTypeParameters;
    protected MethodReference FromNative;

    protected MethodReference CopyContainerMarshallerCtor;
    protected MethodReference CopyFromNative;
    protected MethodReference CopyToNative;
    protected MethodReference CopyDestructInstance;

    protected FieldDefinition NativePropertyField;
    
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
        ContainerMarshallerTypeParameters = [WeaverHelper.UserAssembly.MainModule.ImportReference(InnerProperty.PropertyDataType.CSharpType)];
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

    public override void PrepareForRewrite(TypeDefinition typeDefinition, FunctionMetaData? functionMetadata, PropertyMetaData propertyMetadata)
    {
        base.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);
        InnerProperty.PropertyDataType.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);
        
        PropertyDefinition propertyDef = propertyMetadata.FindPropertyDefinition(typeDefinition);
        
        // Ensure that IList<T> itself is imported.
        WeaverHelper.ImportType(CSharpType);

        InnerProperty.PropertyDataType.PrepareForRewrite(typeDefinition, functionMetadata, InnerProperty);
        InitializeMarshallerParameters();
        
        // Instantiate generics for the direct access and copying marshallers.
        string marshallerTypeName = GetContainerMarshallerName();
        string copyMarshallerTypeName = GetCopyContainerMarshallerName();
        
        var genericMarshallerTypeRef = (from type in WeaverHelper.BindingsAssembly.MainModule.Types
            where type.Namespace == WeaverHelper.UnrealSharpNamespace
                  && type.Name == marshallerTypeName
            select type).Single();
        var genericCopyMarshallerTypeRef = (from type in WeaverHelper.BindingsAssembly.MainModule.Types
            where type.Namespace == WeaverHelper.UnrealSharpNamespace
                  && type.Name == copyMarshallerTypeName
            select type).Single();

        ContainerMarshallerType = WeaverHelper.UserAssembly.MainModule.ImportReference(genericMarshallerTypeRef.Resolve().MakeGenericInstanceType(ContainerMarshallerTypeParameters));
        CopyContainerMarshallerType = WeaverHelper.UserAssembly.MainModule.ImportReference(genericCopyMarshallerTypeRef.Resolve().MakeGenericInstanceType(ContainerMarshallerTypeParameters));
        
        TypeDefinition arrTypeDef = ContainerMarshallerType.Resolve();
        FromNative = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in arrTypeDef.GetMethods() where method.Name == "FromNative" select method).Single());
        FromNative = FunctionProcessor.MakeMethodDeclaringTypeGeneric(FromNative, ContainerMarshallerTypeParameters);

        TypeDefinition copyMarshallerDef = CopyContainerMarshallerType.Resolve();
        CopyContainerMarshallerCtor = copyMarshallerDef.GetConstructors().Single();
        CopyContainerMarshallerCtor = WeaverHelper.UserAssembly.MainModule.ImportReference(CopyContainerMarshallerCtor);
        CopyContainerMarshallerCtor = FunctionProcessor.MakeMethodDeclaringTypeGeneric(CopyContainerMarshallerCtor, ContainerMarshallerTypeParameters);
        
        CopyFromNative = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in copyMarshallerDef.GetMethods() where method.Name == "FromNative" select method).Single());
        CopyFromNative = FunctionProcessor.MakeMethodDeclaringTypeGeneric(CopyFromNative, ContainerMarshallerTypeParameters);
        
        string containerWrapperType = GetContainerWrapperType();
        CopyToNative = WeaverHelper.UserAssembly.MainModule.ImportReference((
            from method in copyMarshallerDef.GetMethods()
            where method.Name == "ToNative"
                && method.Parameters[2].ParameterType is GenericInstanceType genericInstanceType
                && genericInstanceType.ElementType.FullName == containerWrapperType
            select method
        ).Single());
        
        CopyToNative = FunctionProcessor.MakeMethodDeclaringTypeGeneric(CopyToNative, ContainerMarshallerTypeParameters);
        
        CopyDestructInstance = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in copyMarshallerDef.GetMethods() where method.Name == "DestructInstance" select method).Single());
        CopyDestructInstance = FunctionProcessor.MakeMethodDeclaringTypeGeneric(CopyDestructInstance, ContainerMarshallerTypeParameters);

        // If this is a rewritten autoproperty, we need an additional backing field for the Container marshaller.
        // Otherwise, we're copying data for a struct UProp, parameter, or return value.
        string prefix = propertyMetadata.Name + "_";
        
        if (propertyDef != null)
        {
            // Add a field to store the Container marshaller for the getter.                
            ContainerMarshallerField = new FieldDefinition(prefix + "Marshaller", FieldAttributes.Private, ContainerMarshallerType);
            propertyDef.DeclaringType.Fields.Add(ContainerMarshallerField);

            // Suppress the setter.  All modifications should be done by modifying the IList<T> returned by the getter,
            // which will propagate the changes to the underlying native TContainer memory.
            propertyDef.DeclaringType.Methods.Remove(propertyDef.SetMethod);
            propertyDef.SetMethod = null;
            return;
        }

        if (functionMetadata != null)
        {
            prefix = functionMetadata.Name + "_" + prefix;

            ContainerMarshallerField = new FieldDefinition(prefix + "Marshaller", FieldAttributes.Private | FieldAttributes.Static, CopyContainerMarshallerType);
            functionMetadata.MethodDefinition.DeclaringType.Fields.Add(ContainerMarshallerField);
        }

        NativePropertyField = WeaverHelper.FindFieldInType(typeDefinition, prefix + "NativeProperty").Resolve();
    }
    
    

    protected override void CreateGetter(TypeDefinition type, MethodDefinition getter, FieldDefinition offsetField, FieldDefinition nativePropertyField)
    {
        ILProcessor processor = InitPropertyAccessor(getter);

        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldfld, ContainerMarshallerField);

        // Save the position of the branch instruction for later, when we have a reference to its target.
        processor.Emit(OpCodes.Ldarg_0);
        Instruction branchPosition = processor.Body.Instructions[^1];

        processor.Emit(OpCodes.Ldc_I4_1);
        processor.Emit(OpCodes.Ldsfld, nativePropertyField);
        EmitDynamicArrayMarshallerDelegates(processor, type);

        var constructor = ContainerMarshallerType.Resolve().GetConstructors().Single();
        processor.Emit(OpCodes.Newobj, FunctionProcessor.MakeMethodDeclaringTypeGeneric(WeaverHelper.UserAssembly.MainModule.ImportReference(constructor), ContainerMarshallerTypeParameters));
        processor.Emit(OpCodes.Stfld, ContainerMarshallerField);

        // Store the branch destination
        processor.Emit(OpCodes.Ldarg_0);
        Instruction branchTarget = processor.Body.Instructions[^1];
        processor.Emit(OpCodes.Ldfld, ContainerMarshallerField);
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Call, WeaverHelper.NativeObjectGetter);
        processor.Emit(OpCodes.Ldsfld, offsetField);
        processor.Emit(OpCodes.Call, WeaverHelper.IntPtrAdd);
        processor.Emit(OpCodes.Ldc_I4_0);
        processor.Emit(OpCodes.Callvirt, FromNative);
        
        //Now insert the branch
        Instruction branchInstruction = processor.Create(OpCodes.Brtrue_S, branchTarget);
        processor.InsertBefore(branchPosition, branchInstruction);

        EndSimpleGetter(processor, getter);
    }

    protected override void CreateSetter(TypeDefinition type, MethodDefinition setter, FieldDefinition offsetField,
        FieldDefinition nativePropertyField)
    {
        
    }

    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBufferInstruction,
        FieldDefinition offsetField, VariableDefinition localVar)
    {
        WriteMarshalFromNative(processor, type, GetArgumentBufferInstructions(processor, loadBufferInstruction, offsetField), processor.Create(OpCodes.Ldc_I4_0));
        processor.Emit(OpCodes.Stloc, localVar);
    }
    
    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBufferInstruction,
        FieldDefinition offsetField, FieldDefinition destField)
    {
        WriteMarshalFromNative(processor, type, GetArgumentBufferInstructions(processor, loadBufferInstruction, offsetField), processor.Create(OpCodes.Ldc_I4_0));
        processor.Emit(OpCodes.Stfld, destField);
    }

    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type,
        Instruction loadBufferInstruction, FieldDefinition offsetField, int argIndex,
        ParameterDefinition paramDefinition)
    {
        Instruction[] loadSource = argIndex switch
        {
            0 => [processor.Create(OpCodes.Ldarg_0)],
            1 => [processor.Create(OpCodes.Ldarg_1)],
            2 => [processor.Create(OpCodes.Ldarg_2)],
            3 => [processor.Create(OpCodes.Ldarg_3)],
            _ => [processor.Create(OpCodes.Ldarg_S, (byte)argIndex)],
        };

        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBufferInstruction, offsetField);
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), loadSource);
    }
    
    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type,
        Instruction loadBufferInstruction, FieldDefinition offsetField, FieldDefinition srcField)
    {
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBufferInstruction, offsetField);
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), [processor.Create(OpCodes.Ldfld, srcField)]);
    }

    public override void WriteMarshalFromNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr,
        Instruction loadContainerIndex)
    {
        WriteInitCopyMarshaller(processor, type);

        processor.Emit(OpCodes.Ldsfld, ContainerMarshallerField);
        foreach (Instruction inst in loadBufferPtr)
        {
            processor.Body.Instructions.Add(inst);
        }
        processor.Body.Instructions.Add(loadContainerIndex);
        processor.Emit(OpCodes.Callvirt, CopyFromNative);
    }

    public override void WriteMarshalToNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr,
        Instruction loadContainerIndex, Instruction[] loadSource)
    {
        WriteInitCopyMarshaller(processor, type);

        processor.Emit(OpCodes.Ldsfld, ContainerMarshallerField);
        foreach (var i in loadBufferPtr)
        {
            processor.Append(i);
        }
        processor.Append(loadContainerIndex);
        foreach( var i in loadSource)
        {
            processor.Append(i);
        }
        processor.Emit(OpCodes.Callvirt, CopyToNative);
    }

    public override IList<Instruction>? WriteMarshalToNativeWithCleanup(ILProcessor processor, TypeDefinition type,
        Instruction[] loadBufferPtr, Instruction loadContainerIndex, Instruction[] loadSource)
    {
        WriteMarshalToNative(processor, type, loadBufferPtr, loadContainerIndex, loadSource);

        return
        [
            processor.Create(OpCodes.Ldsfld, ContainerMarshallerField),
            .. loadBufferPtr,
            processor.Create(OpCodes.Ldc_I4_0),
            processor.Create(OpCodes.Call, CopyDestructInstance),
        ];
    }

    private void WriteInitCopyMarshaller(ILProcessor processor, TypeDefinition type)
    {
        processor.Emit(OpCodes.Ldsfld, ContainerMarshallerField);

        // Save the position of the branch instruction for later, when we have a reference to its target.
        Instruction branchPosition = processor.Body.Instructions[^1];

        processor.Emit(OpCodes.Ldsfld, NativePropertyField);
        EmitDynamicArrayMarshallerDelegates(processor, type);
        processor.Emit(OpCodes.Newobj, CopyContainerMarshallerCtor);
        processor.Emit(OpCodes.Stsfld, ContainerMarshallerField);

        // Store the branch destination
        processor.Emit(OpCodes.Nop);
        Instruction branchTarget = processor.Body.Instructions[^1];

        //Now insert the branch
        Instruction branchInstruction = processor.Create(OpCodes.Brtrue_S, branchTarget);
        processor.InsertAfter(branchPosition, branchInstruction);
    }
}