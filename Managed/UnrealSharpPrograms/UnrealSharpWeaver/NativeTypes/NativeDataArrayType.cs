using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataArrayType : NativeDataType
{
    public PropertyMetaData InnerProperty { get; set; }

    private TypeReference ArrayMarshallerType;
    private TypeReference CopyArrayMarshallerType;
    private FieldDefinition ArrayMarshallerField;
    private TypeReference[] ArrayMarshallerTypeParameters;
    private MethodReference FromNative;

    private MethodReference CopyArrayMarshallerCtor;
    private MethodReference CopyFromNative;
    private MethodReference CopyToNative;
    private MethodReference CopyDestructInstance;

    private FieldDefinition NativePropertyField;

    public NativeDataArrayType(TypeReference arrayType, int arrayDim, TypeReference innerType) : base(arrayType, arrayDim, PropertyType.Array)
    {
        InnerProperty = PropertyMetaData.FromTypeReference(innerType, "Inner");
        NeedsNativePropertyField = true;
    }

    public override void EmitFixedArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type)
    {
        throw new NotImplementedException("Fixed-size arrays of dynamic arrays not yet supported.");
    }

    public override void PrepareForRewrite(TypeDefinition typeDefinition, FunctionMetaData? functionMetadata, PropertyMetaData propertyMetadata)
    {
        base.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);
        
        PropertyDefinition propertyDef = propertyMetadata.FindPropertyDefinition(typeDefinition);

        // Ensure that IList<T> itself is imported.
        WeaverHelper.UserAssembly.MainModule.ImportReference(CSharpType);

        if (InnerProperty.PropertyDataType is not NativeDataSimpleType)
        {
            //throw new InvalidPropertyException(propertyMetadata.Name, ErrorEmitter.GetSequencePointFromMemberDefinition(typeDefinition), "Only UObjectProperty, UStructProperty, and blittable property types are supported as array inners.");
        }

        InnerProperty.PropertyDataType.PrepareForRewrite(typeDefinition, functionMetadata, InnerProperty);

        // Instantiate generics for the direct access and copying marshallers.
        string marshallerTypeName = "UnrealArrayReadWriteMarshaller`1";
        string copyMarshallerTypeName = "UnrealArrayCopyMarshaller`1";

        ArrayMarshallerTypeParameters = [WeaverHelper.UserAssembly.MainModule.ImportReference(InnerProperty.PropertyDataType.CSharpType)
        ];

        var genericMarshallerTypeRef = (from type in WeaverHelper.BindingsAssembly.MainModule.Types
                                        where type.Namespace == WeaverHelper.UnrealSharpNamespace
                                              && type.Name == marshallerTypeName
                                        select type).Single();
        var genericCopyMarshallerTypeRef = (from type in WeaverHelper.BindingsAssembly.MainModule.Types
                                            where type.Namespace == WeaverHelper.UnrealSharpNamespace
                                                  && type.Name == copyMarshallerTypeName
                                            select type).Single();

        ArrayMarshallerType = WeaverHelper.UserAssembly.MainModule.ImportReference(genericMarshallerTypeRef.Resolve().MakeGenericInstanceType(ArrayMarshallerTypeParameters));
        CopyArrayMarshallerType = WeaverHelper.UserAssembly.MainModule.ImportReference(genericCopyMarshallerTypeRef.Resolve().MakeGenericInstanceType(ArrayMarshallerTypeParameters));

        TypeDefinition arrTypeDef = ArrayMarshallerType.Resolve();
        FromNative = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in arrTypeDef.GetMethods() where method.Name == "FromNative" select method).Single());
        FromNative = FunctionRewriterHelpers.MakeMethodDeclaringTypeGeneric(FromNative, ArrayMarshallerTypeParameters);

        // TODO: Figure out if this should be a IReadOnlyList or a ReadOnlySpan
        string arrayWrapperType = "System.Collections.Generic.IList`1";

        TypeDefinition copyArrTypeDef = CopyArrayMarshallerType.Resolve();
        CopyArrayMarshallerCtor = (from method in copyArrTypeDef.GetConstructors()
                                   where (!method.IsStatic
                                          && method.HasParameters
                                          && method.Parameters.Count == 3
                                          && method.Parameters[0].ParameterType.FullName == "System.IntPtr"
                                          && ((GenericInstanceType)method.Parameters[1].ParameterType).GetElementType().FullName == "UnrealSharp.MarshalingDelegates`1/ToNative"
                                          && ((GenericInstanceType)method.Parameters[2].ParameterType).GetElementType().FullName == "UnrealSharp.MarshalingDelegates`1/FromNative")
                                   select method).Single();
        CopyArrayMarshallerCtor = WeaverHelper.UserAssembly.MainModule.ImportReference(CopyArrayMarshallerCtor);
        CopyArrayMarshallerCtor = FunctionRewriterHelpers.MakeMethodDeclaringTypeGeneric(CopyArrayMarshallerCtor, ArrayMarshallerTypeParameters);
        CopyFromNative = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in copyArrTypeDef.GetMethods() where method.Name == "FromNative" select method).Single());
        CopyFromNative = FunctionRewriterHelpers.MakeMethodDeclaringTypeGeneric(CopyFromNative, ArrayMarshallerTypeParameters);
        CopyToNative = WeaverHelper.UserAssembly.MainModule.ImportReference((
            from method in copyArrTypeDef.GetMethods()
            where method.Name == "ToNative"
                && method.Parameters[2].ParameterType is GenericInstanceType genericInstanceType
                && genericInstanceType.ElementType.FullName == arrayWrapperType
            select method
        ).Single());
        CopyToNative = FunctionRewriterHelpers.MakeMethodDeclaringTypeGeneric(CopyToNative, ArrayMarshallerTypeParameters);
        CopyDestructInstance = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in copyArrTypeDef.GetMethods() where method.Name == "DestructInstance" select method).Single());
        CopyDestructInstance = FunctionRewriterHelpers.MakeMethodDeclaringTypeGeneric(CopyDestructInstance, ArrayMarshallerTypeParameters);

        // If this is a rewritten autoproperty, we need an additional backing field for the array marshaller.
        // Otherwise, we're copying data for a struct UProp, parameter, or return value.
        string prefix = propertyMetadata.Name + "_";
        
        if (propertyDef != null)
        {
            // Add a field to store the array marshaller for the getter.                
            ArrayMarshallerField = new FieldDefinition(prefix + "Marshaller", FieldAttributes.Private, ArrayMarshallerType);
            propertyDef.DeclaringType.Fields.Add(ArrayMarshallerField);

            // Suppress the setter.  All modifications should be done by modifying the IList<T> returned by the getter,
            // which will propagate the changes to the underlying native TArray memory.
            propertyDef.DeclaringType.Methods.Remove(propertyDef.SetMethod);
            propertyDef.SetMethod = null;
            return;
        }

        if (functionMetadata != null)
        {
            prefix = functionMetadata.Name + "_" + prefix;

            ArrayMarshallerField = new FieldDefinition(prefix + "Marshaller", FieldAttributes.Private | FieldAttributes.Static, CopyArrayMarshallerType);
            functionMetadata.MethodDefinition.DeclaringType.Fields.Add(ArrayMarshallerField);
        }

        NativePropertyField = WeaverHelper.FindFieldInType(typeDefinition, prefix + "NativeProperty").Resolve();
    }
    
    protected override void CreateGetter(TypeDefinition type, MethodDefinition getter, FieldDefinition offsetField, FieldDefinition nativePropertyField)
    {
        ILProcessor processor = InitPropertyAccessor(getter);

        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldfld, ArrayMarshallerField);

        // Save the position of the branch instruction for later, when we have a reference to its target.
        processor.Emit(OpCodes.Ldarg_0);
        Instruction branchPosition = processor.Body.Instructions[^1];

        processor.Emit(OpCodes.Ldc_I4_1);
        processor.Emit(OpCodes.Ldsfld, nativePropertyField);
        InnerProperty.PropertyDataType.EmitDynamicArrayMarshallerDelegates(processor, type);

        var constructor = (from method in ArrayMarshallerType.Resolve().GetConstructors()
            where (!method.IsStatic
                   && method.HasParameters
                   && method.Parameters.Count == 4
                   && method.Parameters[0].ParameterType.FullName == "System.Int32"
                   && method.Parameters[1].ParameterType.FullName == "System.IntPtr"
                   && ((GenericInstanceType)method.Parameters[2].ParameterType).GetElementType().FullName == "UnrealSharp.MarshalingDelegates`1/ToNative"
                   && ((GenericInstanceType)method.Parameters[3].ParameterType).GetElementType().FullName == "UnrealSharp.MarshalingDelegates`1/FromNative")
            select method).First();
        processor.Emit(OpCodes.Newobj, FunctionRewriterHelpers.MakeMethodDeclaringTypeGeneric(WeaverHelper.UserAssembly.MainModule.ImportReference(constructor), ArrayMarshallerTypeParameters));
        processor.Emit(OpCodes.Stfld, ArrayMarshallerField);

        // Store the branch destination
        processor.Emit(OpCodes.Ldarg_0);
        Instruction branchTarget = processor.Body.Instructions[^1];
        processor.Emit(OpCodes.Ldfld, ArrayMarshallerField);
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
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBufferInstruction, offsetField);
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), [processor.Create(OpCodes.Ldarg_S, argIndex)]);
    }
    
    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type,
        Instruction loadBufferInstruction, FieldDefinition offsetField, FieldDefinition srcField)
    {
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBufferInstruction, offsetField);
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), [processor.Create(OpCodes.Ldfld, srcField)]);
    }

    public override void WriteMarshalFromNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr,
        Instruction loadArrayIndex)
    {
        WriteInitCopyMarshaller(processor, type);

        processor.Emit(OpCodes.Ldsfld, ArrayMarshallerField);
        foreach (Instruction inst in loadBufferPtr)
        {
            processor.Body.Instructions.Add(inst);
        }
        processor.Body.Instructions.Add(loadArrayIndex);
        processor.Emit(OpCodes.Callvirt, CopyFromNative);
    }

    public override void WriteMarshalToNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr,
        Instruction loadArrayIndex, Instruction[] loadSource)
    {
        WriteInitCopyMarshaller(processor, type);

        processor.Emit(OpCodes.Ldsfld, ArrayMarshallerField);
        foreach (var i in loadBufferPtr)
        {
            processor.Append(i);
        }
        processor.Append(loadArrayIndex);
        foreach( var i in loadSource)
        {
            processor.Append(i);
        }
        processor.Emit(OpCodes.Callvirt, CopyToNative);
    }

    public override IList<Instruction>? WriteMarshalToNativeWithCleanup(ILProcessor processor, TypeDefinition type,
        Instruction[] loadBufferPtr, Instruction loadArrayIndex, Instruction[] loadSource)
    {
        WriteMarshalToNative(processor, type, loadBufferPtr, loadArrayIndex, loadSource);

        string arrayMarshallerFieldName = processor.Body.Method.Name + "_" + "argname" + "_Marshaller";
        var arrayMarshallerField = processor.Body.Method.DeclaringType.Fields.Single(x => x.Name == arrayMarshallerFieldName);

        return
        [
            .. loadBufferPtr,
            processor.Create(OpCodes.Ldsfld, arrayMarshallerField),
            processor.Create(OpCodes.Ldc_I4_0),
            processor.Create(OpCodes.Call, CopyDestructInstance),
        ];
    }

    private void WriteInitCopyMarshaller(ILProcessor processor, TypeDefinition type)
    {
        processor.Emit(OpCodes.Ldsfld, ArrayMarshallerField);

        // Save the position of the branch instruction for later, when we have a reference to its target.
        Instruction branchPosition = processor.Body.Instructions[^1];

        processor.Emit(OpCodes.Ldsfld, NativePropertyField);
        InnerProperty.PropertyDataType.EmitDynamicArrayMarshallerDelegates(processor, type);
        processor.Emit(OpCodes.Newobj, CopyArrayMarshallerCtor);
        processor.Emit(OpCodes.Stsfld, ArrayMarshallerField);

        // Store the branch destination
        processor.Emit(OpCodes.Nop);
        Instruction branchTarget = processor.Body.Instructions[^1];

        //Now insert the branch
        Instruction branchInstruction = processor.Create(OpCodes.Brtrue_S, branchTarget);
        processor.InsertAfter(branchPosition, branchInstruction);
    }
}