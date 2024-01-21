using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Rewriters;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataArrayType : NativeDataType
{
    public PropertyMetaData InnerProperty { get; set; }

    private TypeReference ArrayWrapperType;
    private TypeReference CopyArrayWrapperType;
    private FieldDefinition ArrayWrapperField;
    private TypeReference[] ArrayWrapperTypeParameters;
    private MethodReference FromNative;

    private MethodReference CopyArrayWrapperCtor;
    private MethodReference CopyFromNative;
    private MethodReference CopyToNative;
    private MethodReference CopyDestructInstance;

    private VariableDefinition MarshalingLocal;
    private FieldDefinition ElementSizeField;

    public NativeDataArrayType(TypeReference arrayType, int arrayDim, TypeReference innerType) : base(arrayType, "ArrayProperty", arrayDim, PropertyType.Array)
    {
        InnerProperty = PropertyMetaData.FromTypeReference(innerType, "Inner");
        NeedsNativePropertyField = true;
        NeedsElementSizeField = true;
    }

    public override void EmitFixedArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type)
    {
        throw new NotImplementedException("Fixed-size arrays of dynamic arrays not yet supported.");
    }

    public override void PrepareForRewrite(TypeDefinition typeDefinition, FunctionMetaData? functionMetadata,
        PropertyMetaData propertyMetadata)
    {
        base.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);
        
        PropertyDefinition propertyDef = propertyMetadata.FindPropertyDefinition(typeDefinition);

        // Ensure that IList<T> itself is imported.
        WeaverHelper.UserAssembly.MainModule.ImportReference(CSharpType);

        if (InnerProperty.PropertyDataType is not NativeDataSimpleType)
        {
            throw new InvalidPropertyException(propertyMetadata.Name, ErrorEmitter.GetSequencePointFromMemberDefinition(typeDefinition), "Only UObjectProperty, UStructProperty, and blittable property types are supported as array inners.");
        }

        InnerProperty.PropertyDataType.PrepareForRewrite(typeDefinition, functionMetadata, InnerProperty);

        // Instantiate generics for the direct access and copying marshalers.
        string wrapperTypeName = "UnrealArrayReadWriteMarshaler`1";
        string copyWrapperTypeName = "UnrealArrayCopyMarshaler`1";

        ArrayWrapperTypeParameters = [WeaverHelper.UserAssembly.MainModule.ImportReference(InnerProperty.PropertyDataType.CSharpType)
        ];

        var genericWrapperTypeRef = (from type in WeaverHelper.BindingsAssembly.MainModule.Types
            where type.Namespace == Program.UnrealSharpNamespace
                  && type.Name == wrapperTypeName
            select type).ToArray()[0];
        var genericCopyWrapperTypeRef = (from type in WeaverHelper.BindingsAssembly.MainModule.Types
            where type.Namespace == Program.UnrealSharpNamespace
                  && type.Name == copyWrapperTypeName
            select type).ToArray()[0];

        ArrayWrapperType = WeaverHelper.UserAssembly.MainModule.ImportReference(genericWrapperTypeRef.Resolve().MakeGenericInstanceType(ArrayWrapperTypeParameters));
        CopyArrayWrapperType = WeaverHelper.UserAssembly.MainModule.ImportReference(genericCopyWrapperTypeRef.Resolve().MakeGenericInstanceType(ArrayWrapperTypeParameters));

        TypeDefinition arrTypeDef = ArrayWrapperType.Resolve();
        FromNative = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in arrTypeDef.GetMethods() where method.Name == "FromNative" select method).ToArray()[0]);
        FromNative = FunctionRewriterHelpers.MakeMethodDeclaringTypeGeneric(FromNative, ArrayWrapperTypeParameters);

        TypeDefinition copyArrTypeDef = CopyArrayWrapperType.Resolve();
        CopyArrayWrapperCtor = (from method in copyArrTypeDef.GetConstructors()
            where (!method.IsStatic
                   && method.HasParameters
                   && method.Parameters.Count == 4
                   && method.Parameters[0].ParameterType.FullName == "System.Int32"
                   && ((GenericInstanceType)method.Parameters[1].ParameterType).GetElementType().FullName == "UnrealSharp.MarshalingDelegates`1/ToNative"
                   && ((GenericInstanceType)method.Parameters[2].ParameterType).GetElementType().FullName == "UnrealSharp.MarshalingDelegates`1/FromNative"
                   && method.Parameters[3].ParameterType.FullName == "System.Int32")
            select method).ToArray()[0];
        CopyArrayWrapperCtor = WeaverHelper.UserAssembly.MainModule.ImportReference(CopyArrayWrapperCtor);
        CopyArrayWrapperCtor = FunctionRewriterHelpers.MakeMethodDeclaringTypeGeneric(CopyArrayWrapperCtor, ArrayWrapperTypeParameters);
        CopyFromNative = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in copyArrTypeDef.GetMethods() where method.Name == "FromNative" select method).ToArray()[0]);
        CopyFromNative = FunctionRewriterHelpers.MakeMethodDeclaringTypeGeneric(CopyFromNative, ArrayWrapperTypeParameters);
        CopyToNative = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in copyArrTypeDef.GetMethods() where method.Name == "ToNative" select method).ToArray()[0]);
        CopyToNative = FunctionRewriterHelpers.MakeMethodDeclaringTypeGeneric(CopyToNative, ArrayWrapperTypeParameters);
        CopyDestructInstance = WeaverHelper.UserAssembly.MainModule.ImportReference((from method in copyArrTypeDef.GetMethods() where method.Name == "DestructInstance" select method).ToArray()[0]);
        CopyDestructInstance = FunctionRewriterHelpers.MakeMethodDeclaringTypeGeneric(CopyDestructInstance, ArrayWrapperTypeParameters);

        // If this is a rewritten autoproperty, we need an additional backing field for the array wrapper.
        // Otherwise, we're copying data for a struct UProp, parameter, or return value.
        string prefix = propertyMetadata.Name + "_";
        
        if (propertyDef != null)
        {
            // Add a field to store the array wrapper for the getter.                
            ArrayWrapperField = new FieldDefinition(prefix + "Wrapper", FieldAttributes.Private, ArrayWrapperType);
            propertyDef.DeclaringType.Fields.Add(ArrayWrapperField);

            // Suppress the setter.  All modifications should be done by modifying the IList<T> returned by the getter,
            // which will propagate the changes to the underlying native TArray memory.
            propertyDef.DeclaringType.Methods.Remove(propertyDef.SetMethod);
            propertyDef.SetMethod = null;
            return;
        }

        if (functionMetadata != null)
        {
            prefix = functionMetadata.Name + "_" + prefix;
        }
        
        MarshalingLocal = new VariableDefinition(CopyArrayWrapperType);
        ElementSizeField = WeaverHelper.FindFieldInType(typeDefinition, prefix + "ElementSize").Resolve();
    }
    
    protected override void CreateGetter(TypeDefinition type, MethodDefinition getter, FieldDefinition offsetField, FieldDefinition nativePropertyField)
    {
        ILProcessor processor = InitPropertyAccessor(getter);

        /*
            .method public hidebysig specialname instance class [mscorlib]System.Collections.Generic.IList`1<valuetype [UnrealEngine.Runtime]UnrealEngine.Runtime.Name>
                        get_Tags() cil managed
                {
                  // Code size       79 (0x4f)
                  .maxstack  6
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      class [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealArrayReadWriteMarshaler`1<valuetype [UnrealEngine.Runtime]UnrealEngine.Runtime.Name> UnrealEngine.Engine.Actor::Tags_Wrapper
                  IL_0006:  brtrue.s   IL_0031
                  IL_0008:  ldarg.0
                  IL_0009:  ldc.i4.1
                  IL_000a:  ldsfld     native int UnrealEngine.Engine.Actor::Tags_NativeProperty
                  IL_000f:  ldnull
                  IL_0010:  ldftn      void class [UnrealEngine.Runtime]UnrealEngine.Runtime.BlittableMarshaller`1<valuetype [UnrealEngine.Runtime]UnrealEngine.Runtime.Name>::ToNative(native int,
                                                                                                                                                                                           int32,
                                                                                                                                                                                           class [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealObject,
                                                                                                                                                                                           !0)
                  IL_0016:  newobj     instance void class [UnrealEngine.Runtime]UnrealEngine.Runtime.MarshalingDelegates`1/ToNative<valuetype [UnrealEngine.Runtime]UnrealEngine.Runtime.Name>::.ctor(object,
                                                                                                                                                                                                       native int)
                  IL_001b:  ldnull
                  IL_001c:  ldftn      !0 class [UnrealEngine.Runtime]UnrealEngine.Runtime.BlittableMarshaller`1<valuetype [UnrealEngine.Runtime]UnrealEngine.Runtime.Name>::FromNative(native int,
                                                                                                                                                                                           int32,
                                                                                                                                                                                           class [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealObject)
                  IL_0022:  newobj     instance void class [UnrealEngine.Runtime]UnrealEngine.Runtime.MarshalingDelegates`1/FromNative<valuetype [UnrealEngine.Runtime]UnrealEngine.Runtime.Name>::.ctor(object,
                                                                                                                                                                                                         native int)
                  IL_0027:  newobj     instance void class [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealArrayReadWriteMarshaler`1<valuetype [UnrealEngine.Runtime]UnrealEngine.Runtime.Name>::.ctor(int32,
                                                                                                                                                                                                        native int,
                                                                                                                                                                                                        class [UnrealEngine.Runtime]UnrealEngine.Runtime.MarshalingDelegates`1/ToNative<!0>,
                                                                                                                                                                                                        class [UnrealEngine.Runtime]UnrealEngine.Runtime.MarshalingDelegates`1/FromNative<!0>)
                  IL_002c:  stfld      class [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealArrayReadWriteMarshaler`1<valuetype [UnrealEngine.Runtime]UnrealEngine.Runtime.Name> UnrealEngine.Engine.Actor::Tags_Wrapper
                  IL_0031:  ldarg.0
                  IL_0032:  ldfld      class [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealArrayReadWriteMarshaler`1<valuetype [UnrealEngine.Runtime]UnrealEngine.Runtime.Name> UnrealEngine.Engine.Actor::Tags_Wrapper
                  IL_0037:  ldarg.0
                  IL_0038:  call       instance native int [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealObject::get_NativeObject()
                  IL_003d:  ldsfld     int32 UnrealEngine.Engine.Actor::Tags_Offset
                  IL_0042:  call       native int [mscorlib]System.IntPtr::Add(native int,
                                                                               int32)
                  IL_0047:  ldc.i4.0
                  IL_0048:  ldarg.0
                  IL_0049:  callvirt   instance class [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealArrayReadWrite`1<!0> class [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealArrayReadWriteMarshaler`1<valuetype [UnrealEngine.Runtime]UnrealEngine.Runtime.Name>::FromNative(native int,
                                                                                                                                                                                                                                                                                    int32,
                                                                                                                                                                                                                                                                                    class [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealObject)
                  IL_004e:  ret
                } // end of method Actor::get_Tags
         */


        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldfld, ArrayWrapperField);

        // Save the position of the branch instruction for later, when we have a reference to its target.
        processor.Emit(OpCodes.Ldarg_0);
        Instruction branchPosition = processor.Body.Instructions[processor.Body.Instructions.Count - 1];

        processor.Emit(OpCodes.Ldc_I4_1);
        processor.Emit(OpCodes.Ldsfld, nativePropertyField);
        InnerProperty.PropertyDataType.EmitDynamicArrayMarshalerDelegates(processor, type);

        var constructor = (from method in ArrayWrapperType.Resolve().GetConstructors()
            where (!method.IsStatic
                   && method.HasParameters
                   && method.Parameters.Count == 4
                   && method.Parameters[0].ParameterType.FullName == "System.Int32"
                   && method.Parameters[1].ParameterType.FullName == "System.IntPtr"
                   && ((GenericInstanceType)method.Parameters[2].ParameterType).GetElementType().FullName == "UnrealSharp.MarshalingDelegates`1/ToNative"
                   && ((GenericInstanceType)method.Parameters[3].ParameterType).GetElementType().FullName == "UnrealSharp.MarshalingDelegates`1/FromNative")
            select method).ToArray()[0];
        processor.Emit(OpCodes.Newobj, FunctionRewriterHelpers.MakeMethodDeclaringTypeGeneric(WeaverHelper.UserAssembly.MainModule.ImportReference(constructor), ArrayWrapperTypeParameters));
        processor.Emit(OpCodes.Stfld, ArrayWrapperField);

        // Store the branch destination
        processor.Emit(OpCodes.Ldarg_0);
        Instruction branchTarget = processor.Body.Instructions[processor.Body.Instructions.Count - 1];
        processor.Emit(OpCodes.Ldfld, ArrayWrapperField);
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Call, WeaverHelper.NativeObjectGetter);
        processor.Emit(OpCodes.Ldsfld, offsetField);
        processor.Emit(OpCodes.Call, WeaverHelper.IntPtrAdd);
        processor.Emit(OpCodes.Ldc_I4_0);
        processor.Emit(OpCodes.Ldarg_0);
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
        WriteMarshalFromNative(processor, type, GetArgumentBufferInstructions(processor, loadBufferInstruction, offsetField), processor.Create(OpCodes.Ldc_I4_0), processor.Create(OpCodes.Ldnull));
        processor.Emit(OpCodes.Stloc, localVar);
    }
    
    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBufferInstruction,
        FieldDefinition offsetField, FieldDefinition destField)
    {
        WriteMarshalFromNative(processor, type, GetArgumentBufferInstructions(processor, loadBufferInstruction, offsetField), processor.Create(OpCodes.Ldc_I4_0), processor.Create(OpCodes.Ldnull));
        processor.Emit(OpCodes.Stfld, destField);
    }

    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type,
        Instruction loadBufferInstruction, FieldDefinition offsetField, int argIndex,
        ParameterDefinition paramDefinition)
    {
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBufferInstruction, offsetField);
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), processor.Create(OpCodes.Ldnull), new Instruction[] { processor.Create(OpCodes.Ldarg_S, argIndex) });
    }
    
    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type,
        Instruction loadBufferInstruction, FieldDefinition offsetField, FieldDefinition srcField)
    {
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBufferInstruction, offsetField);
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), processor.Create(OpCodes.Ldnull), new Instruction[] { processor.Create(OpCodes.Ldfld, srcField) });
    }

    public override void WriteMarshalFromNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr,
        Instruction loadArrayIndex, Instruction loadOwner)
    {
        processor.Body.Variables.Add(MarshalingLocal);

        processor.Emit(OpCodes.Ldc_I4_1);
        InnerProperty.PropertyDataType.EmitDynamicArrayMarshalerDelegates(processor, type);
        processor.Emit(OpCodes.Ldsfld, ElementSizeField);
        processor.Emit(OpCodes.Newobj, CopyArrayWrapperCtor);
        processor.Emit(OpCodes.Stloc, MarshalingLocal);

        processor.Emit(OpCodes.Ldloc, MarshalingLocal);
        foreach (Instruction inst in loadBufferPtr)
        {
            processor.Body.Instructions.Add(inst);
        }
        processor.Body.Instructions.Add(loadArrayIndex);
        processor.Body.Instructions.Add(loadOwner);
        processor.Emit(OpCodes.Callvirt, CopyFromNative);
    }

    public override void WriteMarshalToNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr,
        Instruction loadArrayIndex, Instruction loadOwner, Instruction[] loadSource)
    {
        processor.Body.Variables.Add(MarshalingLocal);

        processor.Emit(OpCodes.Ldc_I4_1);
        InnerProperty.PropertyDataType.EmitDynamicArrayMarshalerDelegates(processor, type);
        processor.Emit(OpCodes.Ldsfld, ElementSizeField);
        processor.Emit(OpCodes.Newobj, CopyArrayWrapperCtor);
        processor.Emit(OpCodes.Stloc, MarshalingLocal);

        processor.Emit(OpCodes.Ldloc, MarshalingLocal);
        foreach (var i in loadBufferPtr)
        {
            processor.Append(i);
        }
        processor.Append(loadArrayIndex);
        processor.Append(loadOwner);
        foreach( var i in loadSource)
        {
            processor.Append(i);
        }
        processor.Emit(OpCodes.Callvirt, CopyToNative);
    }
    public override IList<Instruction>? WriteMarshalToNativeWithCleanup(ILProcessor processor, TypeDefinition type,
        Instruction[] loadBufferPtr, Instruction loadArrayIndex, Instruction loadOwner, Instruction[] loadSource)
    {
        WriteMarshalToNative(processor, type, loadBufferPtr, loadArrayIndex, loadOwner, loadSource);

        IList<Instruction> cleanupInstructions = new List<Instruction>();
        foreach (var i in loadBufferPtr)
        {
            cleanupInstructions.Add(i);
        }
        cleanupInstructions.Add(processor.Create(OpCodes.Ldloc, MarshalingLocal));
        cleanupInstructions.Add(processor.Create(OpCodes.Ldc_I4_0));
        cleanupInstructions.Add(processor.Create(OpCodes.Call, CopyDestructInstance));
        return cleanupInstructions;
    }
}