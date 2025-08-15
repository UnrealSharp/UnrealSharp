using System.Collections.Immutable;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataContainerType : NativeDataType
{
    public PropertyMetaData InnerProperty { get; set; }

    private TypeReference? _containerMarshallerType;
    private MethodReference? _containerMashallerCtor;
    private FieldDefinition? _containerMarshallerField;

    private MethodReference? _fromNative;
    private MethodReference? _toNative;

    private FieldDefinition? _nativePropertyField;

    private MethodReference? _copyDestructInstance;
    
    protected virtual AssemblyDefinition MarshallerAssembly => WeaverImporter.Instance.UnrealSharpAssembly;
    protected virtual string MarshallerNamespace => WeaverImporter.UnrealSharpNamespace;
    
    protected TypeReference[] ContainerMarshallerTypeParameters { get; set; } = [];
    
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
        ContainerMarshallerTypeParameters = [InnerProperty.PropertyDataType.CSharpType.ImportType()];
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
        CSharpType.ImportType();

        InitializeMarshallerParameters();

        // Instantiate generics for the direct access and copying marshallers.
        string prefix = propertyMetadata.Name + "_";
        
        FieldAttributes fieldAttributes = FieldAttributes.Private;
        if (outer is MethodDefinition method)
        {
            prefix = method.Name + "_" + prefix;
            TypeReference genericCopyMarshallerTypeRef = MarshallerAssembly.FindType(GetCopyContainerMarshallerName(), MarshallerNamespace)!;
            
            _containerMarshallerType = genericCopyMarshallerTypeRef.Resolve().MakeGenericInstanceType(ContainerMarshallerTypeParameters).ImportType();
            
            _copyDestructInstance = _containerMarshallerType.Resolve().FindMethod("DestructInstance")!;
            _copyDestructInstance = FunctionProcessor.MakeMethodDeclaringTypeGeneric(_copyDestructInstance, ContainerMarshallerTypeParameters);
            
            fieldAttributes |= FieldAttributes.Static;
        }
        else
        {
            TypeReference genericCopyMarshallerTypeRef = MarshallerAssembly.FindType(GetContainerMarshallerName(), MarshallerNamespace)!;
            _containerMarshallerType = genericCopyMarshallerTypeRef.Resolve().MakeGenericInstanceType(ContainerMarshallerTypeParameters).ImportType();

            if (propertyMetadata.MemberRef is PropertyDefinition propertyDefinition)
            {
                typeDefinition.Methods.Remove(propertyDefinition.SetMethod);
                propertyDefinition.SetMethod = null;
            }
        }
        
        TypeDefinition arrTypeDef = _containerMarshallerType.Resolve();
        
        _containerMashallerCtor = arrTypeDef.GetConstructors().Single();
        _containerMashallerCtor = _containerMashallerCtor.ImportMethod();
        _containerMashallerCtor = FunctionProcessor.MakeMethodDeclaringTypeGeneric(_containerMashallerCtor, ContainerMarshallerTypeParameters);
        
        _fromNative = arrTypeDef.FindMethod("FromNative")!;
        _fromNative = FunctionProcessor.MakeMethodDeclaringTypeGeneric(_fromNative, ContainerMarshallerTypeParameters);

        _toNative = arrTypeDef.FindMethod("ToNative")!;
        _toNative = FunctionProcessor.MakeMethodDeclaringTypeGeneric(_toNative, ContainerMarshallerTypeParameters);
        
        _containerMarshallerField = typeDefinition.AddField(prefix + "Marshaller", _containerMarshallerType, fieldAttributes);
        _nativePropertyField = propertyMetadata.NativePropertyField!;
    }

    public override void WriteGetter(TypeDefinition type, MethodDefinition getter, Instruction[] loadBufferPtr, FieldDefinition? fieldDefinition)
    {
        ILProcessor processor = InitPropertyAccessor(getter);

        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldfld, _containerMarshallerField);

        // Save the position of the branch instruction for later, when we have a reference to its target.
        processor.Emit(OpCodes.Ldarg_0);
        Instruction branchPosition = processor.Body.Instructions[^1];
        
        processor.Emit(OpCodes.Ldsfld, fieldDefinition);
        EmitDynamicArrayMarshallerDelegates(processor, type);

        if (_containerMarshallerType == null)
        {
            throw new InvalidOperationException("Container marshaller type is null");
        }

        MethodDefinition? constructor = _containerMarshallerType.Resolve().GetConstructors().Single();
        processor.Emit(OpCodes.Newobj, FunctionProcessor.MakeMethodDeclaringTypeGeneric(WeaverImporter.Instance.CurrentWeavingAssembly.MainModule.ImportReference(constructor), ContainerMarshallerTypeParameters));
        processor.Emit(OpCodes.Stfld, _containerMarshallerField);

        // Store the branch destination
        processor.Emit(OpCodes.Ldarg_0);
        Instruction branchTarget = processor.Body.Instructions[^1];
        processor.Emit(OpCodes.Ldfld, _containerMarshallerField);
        
        foreach (Instruction inst in loadBufferPtr)
        {
            processor.Append(inst);
        }
        
        processor.Emit(OpCodes.Ldc_I4_0);
        processor.Emit(OpCodes.Call, _fromNative);
        
        //Now insert the branch
        Instruction branchInstruction = processor.Create(OpCodes.Brtrue_S, branchTarget);
        processor.InsertBefore(branchPosition, branchInstruction);

        getter.FinalizeMethod();
    }

    public override void WriteSetter(TypeDefinition type, MethodDefinition setter, Instruction[] loadBufferPtr,
        FieldDefinition? fieldDefinition)
    {
        ILProcessor processor = BeginSimpleSetter(setter);
        Instruction loadValue = processor.Create(OpCodes.Ldarg_1);
        WriteMarshalToNative(processor, type, loadBufferPtr, processor.Create(OpCodes.Ldc_I4_0), loadValue);
        setter.FinalizeMethod();
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
        processor.Emit(OpCodes.Call, _fromNative);
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
        
        processor.Emit(OpCodes.Call, _toNative);
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
        processor.Emit(OpCodes.Ldsfld, _nativePropertyField);
        
        EmitDynamicArrayMarshallerDelegates(processor, type);
        
        processor.Emit(OpCodes.Newobj, _containerMashallerCtor);
        processor.Emit(OpCodes.Stfld, _containerMarshallerField);
        
        processor.Append(branchTarget);
    }
    
    private void AppendLoadMarshallerInstructions(ILProcessor processor)
    {
        if (_containerMarshallerField == null)
        {
            throw new InvalidOperationException("Container marshaller field is null");
        }
        
        if (_containerMarshallerField.IsStatic)
        {  
            processor.Emit(OpCodes.Ldsfld, _containerMarshallerField);
        }
        else
        {
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldfld, _containerMarshallerField);
        }

    }
}
