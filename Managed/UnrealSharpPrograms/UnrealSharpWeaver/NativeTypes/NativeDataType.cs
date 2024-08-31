using System.Text.Json.Serialization;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace UnrealSharpWeaver.NativeTypes;

[JsonDerivedType(typeof(NativeDataEnumType))]
[JsonDerivedType(typeof(NativeDataNameType))]
[JsonDerivedType(typeof(NativeDataTextType))]
[JsonDerivedType(typeof(NativeDataArrayType))]
[JsonDerivedType(typeof(NativeDataClassType))]
[JsonDerivedType(typeof(NativeDataObjectType))]
[JsonDerivedType(typeof(NativeDataStringType))]
[JsonDerivedType(typeof(NativeDataStructType))]
[JsonDerivedType(typeof(NativeDataBooleanType))]
[JsonDerivedType(typeof(NativeDataBuiltinType))]
[JsonDerivedType(typeof(NativeDataCoreStructType))]
[JsonDerivedType(typeof(NativeDataWeakObjectType))]
[JsonDerivedType(typeof(NativeDataMulticastDelegate))]
[JsonDerivedType(typeof(NativeDataBlittableStructType))]
[JsonDerivedType(typeof(NativeDataDefaultComponent))]
[JsonDerivedType(typeof(NativeDataSoftObjectType))]
[JsonDerivedType(typeof(NativeDataSoftClassType))]
[JsonDerivedType(typeof(NativeDataDelegateType))]
[JsonDerivedType(typeof(NativeDataMapType))]
public abstract class NativeDataType(TypeReference typeRef, int arrayDim, PropertyType propertyType = PropertyType.Unknown)
{
    internal TypeReference CSharpType { get; set; } = WeaverHelper.ImportType(typeRef);
    public int ArrayDim { get; set; } = arrayDim;
    public bool NeedsNativePropertyField { get; set; } 
    public bool NeedsElementSizeField { get; set; }
    public PropertyType PropertyType { get; set; } = propertyType;
    public virtual bool IsBlittable { get { return false; } }
    public virtual bool IsPlainOldData { get { return false; } }
    public bool IsNetworkSupported = true;
    
    // Non-json properties
    // Generic instance type for fixed-size array wrapper. Populated only when ArrayDim > 1.
    protected TypeReference FixedSizeArrayWrapperType;
    
    // Instance backing field for fixed-size array wrapper. Populated only when ArrayDim > 1.
    protected FieldDefinition FixedSizeArrayWrapperField;
    
    protected FieldDefinition? BackingField;

    private TypeReference ToNativeDelegateType;
    private TypeReference FromNativeDelegateType;
    // End non-json properties

    protected static ILProcessor InitPropertyAccessor(MethodDefinition method)
    {
        method.Body = new MethodBody(method);
        method.CustomAttributes.Clear();
        ILProcessor processor = method.Body.GetILProcessor();
        method.Body.Instructions.Clear();
        return processor;
    }
    
    protected void AddBackingField(TypeDefinition type, PropertyMetaData propertyMetaData)
    {
        if (BackingField != null)
        {
            throw new Exception($"Backing field already exists for {propertyMetaData.Name} in {type.FullName}");
        }
        
        BackingField = WeaverHelper.AddFieldToType(type, $"{propertyMetaData.Name}_BackingField", CSharpType, FieldAttributes.Private);
    }
    
    public static Instruction[] GetArgumentBufferInstructions(ILProcessor processor, Instruction? loadBufferInstruction, FieldDefinition offsetField)
    {
        List<Instruction> instructionBuffer = [];
        
        if (loadBufferInstruction != null)
        {
            instructionBuffer.Add(loadBufferInstruction);
        }
        else
        {
            instructionBuffer.Add(processor.Create(OpCodes.Ldarg_0));
            instructionBuffer.Add(processor.Create(OpCodes.Call, WeaverHelper.NativeObjectGetter));
        }

        instructionBuffer.Add(processor.Create(OpCodes.Ldsfld, offsetField));  
        instructionBuffer.Add(processor.Create(OpCodes.Call, WeaverHelper.IntPtrAdd));

        return instructionBuffer.ToArray();
    }

    protected static ILProcessor BeginSimpleGetter(MethodDefinition getter)
    {
        ILProcessor processor = InitPropertyAccessor(getter);
        /*
        .method public hidebysig specialname instance int32
                get_TestReadableInt32() cil managed
        {
          // Code size       25 (0x19)
          .maxstack  2
          .locals init ([0] int32 ToReturn)
          IL_0000:  ldarg.0
          IL_0001:  call       instance native int [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealObject::get_NativeObject()
          IL_0006:  ldsfld     int32 UnrealEngine.MonoRuntime.MonoTestsObject::TestReadableInt32_Offset
          IL_000b:  call       native int [mscorlib]System.IntPtr::Add(native int,
                                                                       int32)
          IL_0010:  call       void* [mscorlib]System.IntPtr::op_Explicit(native int)
          IL_0015:  ldind.i4
          IL_0018:  ret
        } // end of method MonoTestsObject::get_TestReadableInt32
         */
        return processor;
    }

    protected static void EndSimpleGetter(ILProcessor processor, MethodDefinition getter)
    {
        processor.Emit(OpCodes.Ret);
        getter.Body.OptimizeMacros();
    }
    
    protected static ILProcessor BeginSimpleSetter(MethodDefinition setter)
    {
        ILProcessor processor = InitPropertyAccessor(setter);
        /*
         .method public hidebysig specialname instance void
                set_TestReadWriteFloat(float32 'value') cil managed
        {
          // Code size       24 (0x18)
          .maxstack  8
          IL_0000:  ldarg.0
          IL_0001:  call       instance native int [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealObject::get_NativeObject()
          IL_0006:  ldsfld     int32 UnrealEngine.MonoRuntime.MonoTestsObject::TestReadWriteFloat_Offset
          IL_000b:  call       native int [mscorlib]System.IntPtr::Add(native int,
                                                                       int32)
          IL_0010:  call       void* [mscorlib]System.IntPtr::op_Explicit(native int)
          IL_0015:  ldarg.1
          IL_0016:  stind.r4
          IL_0017:  ret
        } // end of method MonoTestsObject::set_TestReadWriteFloat
         */
        return processor;
    }

    public virtual void WritePostInitialization(ILProcessor processor, PropertyMetaData propertyMetadata, Instruction loadNativePointer, Instruction setNativePointer)
    {
        
    }

    protected static void EndSimpleSetter(ILProcessor processor, MethodDefinition setter)
    {
        processor.Emit(OpCodes.Ret);
        setter.Body.OptimizeMacros();
    }

    // Subclasses may override to do additional prep, such as adding additional backing fields.
    public virtual void PrepareForRewrite(TypeDefinition typeDefinition, FunctionMetaData? functionMetadata, PropertyMetaData propertyMetadata)
    {
        PropertyDefinition propertyDef = propertyMetadata.FindPropertyDefinition(typeDefinition);
        
        if (ArrayDim > 1)
        {
            // Suppress the setter.  All modifications should be done by modifying the FixedSizeArray wrapper
            // returned by the getter, which will apply the changes to the underlying native array.
            propertyDef.DeclaringType.Methods.Remove(propertyDef.SetMethod);
            propertyDef.SetMethod = null;

            // Add an instance backing field to hold the fixed-size array wrapper.
            FixedSizeArrayWrapperType = WeaverHelper.FindGenericTypeInAssembly(WeaverHelper.BindingsAssembly, WeaverHelper.UnrealSharpNamespace, "FixedSizeArrayReadWrite`1", [CSharpType]);
            FixedSizeArrayWrapperField = WeaverHelper.AddFieldToType(typeDefinition, propertyDef.Name + "_Marshaller", FixedSizeArrayWrapperType);
        }
        
        var marshallingDelegates = WeaverHelper.FindGenericTypeInAssembly(WeaverHelper.BindingsAssembly, WeaverHelper.UnrealSharpNamespace, "MarshallingDelegates`1", [CSharpType]);
        TypeDefinition marshallingDelegatesDef = marshallingDelegates.Resolve();
        
        ToNativeDelegateType = WeaverHelper.FindNestedType(marshallingDelegatesDef, "ToNative");
        FromNativeDelegateType = WeaverHelper.FindNestedType(marshallingDelegatesDef, "FromNative");
    }

    protected void EmitDelegate(ILProcessor processor, TypeReference delegateType, MethodReference method)
    {
        processor.Emit(OpCodes.Ldnull);
        method = WeaverHelper.UserAssembly.MainModule.ImportReference(method);
        processor.Emit(OpCodes.Ldftn, method);
        MethodReference ctor = (from constructor in delegateType.Resolve().GetConstructors() where constructor.Parameters.Count == 2 select constructor).First().Resolve();
        ctor = FunctionProcessor.MakeMethodDeclaringTypeGeneric(ctor, CSharpType);
        ctor = WeaverHelper.UserAssembly.MainModule.ImportReference(ctor);
        processor.Emit(OpCodes.Newobj, ctor);
    }

    // Emits IL for a default constructible and possibly generic fixed array marshalling helper object.
    // If typeParams is null, a non-generic type is assumed.
    protected void EmitSimpleMarshallerDelegates(ILProcessor processor, string marshallerTypeName, TypeReference[]? typeParams)
    {
        TypeReference? marshallerType = null;
        WeaverHelper.ForEachAssembly(action: assembly =>
        {
            if (typeParams is { Length: > 0 })
            {
                marshallerType = WeaverHelper.FindGenericTypeInAssembly(assembly, string.Empty, marshallerTypeName, typeParams, false);
            }
            else
            {
                marshallerType = WeaverHelper.FindTypeInAssembly(assembly, marshallerTypeName, string.Empty, false);
            }
            return marshallerType == null;
        });
        
        if (marshallerType == null)
        {
            throw new Exception($"Could not find marshaller type {marshallerTypeName} in any assembly.");
        }
        
        TypeDefinition marshallerTypeDef = marshallerType.Resolve();
        MethodReference fromNative = WeaverHelper.FindMethod(marshallerTypeDef, "FromNative")!;
        MethodReference toNative = WeaverHelper.FindMethod(marshallerTypeDef, "ToNative")!;

        if (typeParams != null)
        {
            fromNative = FunctionProcessor.MakeMethodDeclaringTypeGeneric(fromNative, typeParams);
            toNative = FunctionProcessor.MakeMethodDeclaringTypeGeneric(toNative, typeParams);
        }

        EmitDelegate(processor, ToNativeDelegateType, toNative);
        EmitDelegate(processor, FromNativeDelegateType, fromNative);
    }

    public abstract void EmitFixedArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type);
    public virtual void EmitDynamicArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type)
    {
        EmitFixedArrayMarshallerDelegates(processor, type);
    }

    public void WriteGetter(TypeDefinition type, MethodDefinition getter, FieldDefinition offsetField, FieldDefinition nativePropertyField)
    {
        if (ArrayDim == 1)
        {
            CreateGetter(type, getter, offsetField, nativePropertyField);
        }
        else
        {
            ILProcessor processor = InitPropertyAccessor(getter);

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldfld, FixedSizeArrayWrapperField);

            // Store branch position for later insertion
            processor.Emit(OpCodes.Ldarg_0);
            Instruction branchPosition = processor.Body.Instructions[processor.Body.Instructions.Count - 1];
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldsfld, offsetField);
            processor.Emit(OpCodes.Ldc_I4, ArrayDim);

            // Allow subclasses to control construction of their own marshallers, as there may be
            // generics and/or ctor parameters involved.
            EmitFixedArrayMarshallerDelegates(processor, type);
            
            var constructors = (from method in FixedSizeArrayWrapperType.Resolve().GetConstructors()
                where (!method.IsStatic
                       && method.HasParameters
                       && method.Parameters.Count == 5
                       && method.Parameters[0].ParameterType.FullName == "UnrealEngine.Runtime.UnrealObject"
                       && method.Parameters[1].ParameterType.FullName == "System.Int32"
                       && method.Parameters[2].ParameterType.FullName == "System.Int32"
                       && method.Parameters[3].ParameterType.IsGenericInstance
                       && ((GenericInstanceType)method.Parameters[3].ParameterType).GetElementType().FullName == "UnrealEngine.Runtime.MarshallingDelegates`1/ToNative"
                       && ((GenericInstanceType)method.Parameters[4].ParameterType).GetElementType().FullName == "UnrealEngine.Runtime.MarshallingDelegates`1/FromNative")
                select method).ToArray();
            ConstructorBuilder.VerifySingleResult(constructors, type, "FixedSizeArrayWrapper UObject-backed constructor");
            processor.Emit(OpCodes.Newobj, WeaverHelper.UserAssembly.MainModule.ImportReference(FunctionProcessor.MakeMethodDeclaringTypeGeneric(constructors[0], [CSharpType])));
            processor.Emit(OpCodes.Stfld, FixedSizeArrayWrapperField);

            // Store branch target
            processor.Emit(OpCodes.Ldarg_0);
            Instruction branchTarget = processor.Body.Instructions[^1];
            processor.Emit(OpCodes.Ldfld, FixedSizeArrayWrapperField);

            // Insert branch
            processor.InsertBefore(branchPosition, processor.Create(OpCodes.Brtrue, branchTarget));

            EndSimpleGetter(processor, getter);
        }
    }

    public void WriteSetter(TypeDefinition type, MethodDefinition setter, FieldDefinition offsetField, FieldDefinition nativePropertyField)
    {
        if (ArrayDim == 1)
        {
            CreateSetter(type, setter, offsetField, nativePropertyField);
        }
        else
        {
            throw new NotSupportedException("Fixed-size array property setters should be stripped, not rewritten.");
        }
    }

    protected abstract void CreateGetter(TypeDefinition type, MethodDefinition getter, FieldDefinition offsetField, FieldDefinition nativePropertyField);
    protected abstract void CreateSetter(TypeDefinition type, MethodDefinition setter, FieldDefinition offsetField, FieldDefinition nativePropertyField);

    // Subclasses must implement to handle loading of values from a native buffer.
    // Returns the local variable containing the loaded value.
    public abstract void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBufferInstruction, FieldDefinition offsetField, VariableDefinition localVar);
    public abstract void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBufferInstruction, FieldDefinition offsetField, FieldDefinition destField);

    // Subclasses must implement to handle storing of a value into a native buffer.
    // Return value is a list of instructions that must be executed to clean up the value in the buffer, or null if no cleanup is required.
    public abstract IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type,
        Instruction loadBufferInstruction, FieldDefinition offsetField, int argIndex,
        ParameterDefinition paramDefinition);
    public abstract IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type, Instruction loadBufferInstruction, FieldDefinition offsetField, FieldDefinition srcField);

    public abstract void WriteMarshalFromNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr, Instruction loadArrayIndex);
    public abstract void WriteMarshalToNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr, Instruction loadArrayIndex, Instruction[] loadSource);
        
    public void WriteMarshalToNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr, Instruction loadArrayIndex, Instruction loadSource)
    {
        WriteMarshalToNative(processor, type, loadBufferPtr, loadArrayIndex, [loadSource]);
    }

    public virtual IList<Instruction>? WriteMarshalToNativeWithCleanup(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr, Instruction loadArrayIndex, Instruction[] loadSource)
    {
        WriteMarshalToNative(processor, type, loadBufferPtr, loadArrayIndex, loadSource);
        return null;
    }
}

