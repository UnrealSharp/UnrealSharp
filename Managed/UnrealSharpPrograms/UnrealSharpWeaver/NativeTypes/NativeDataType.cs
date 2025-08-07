using System.Text.Json.Serialization;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;
using UnrealSharpWeaver.Utilities;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace UnrealSharpWeaver.NativeTypes;

[JsonDerivedType(typeof(NativeDataEnumType))]
[JsonDerivedType(typeof(NativeDataNameType))]
[JsonDerivedType(typeof(NativeDataTextType))]
[JsonDerivedType(typeof(NativeDataArrayType))]
[JsonDerivedType(typeof(NativeDataNativeArrayType))]
[JsonDerivedType(typeof(NativeDataClassBaseType))]
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
[JsonDerivedType(typeof(NativeDataSetType))]
[JsonDerivedType(typeof(NativeDataClassType))]
[JsonDerivedType(typeof(NativeDataInterfaceType))]
[JsonDerivedType(typeof(NativeDataOptionalType))]
[JsonDerivedType(typeof(NativeDataManagedObjectType))]
[JsonDerivedType(typeof(NativeDataUnmanagedType))]
public abstract class NativeDataType
{
    internal TypeReference CSharpType { get; set; }
    public int ArrayDim { get; set; }
    public PropertyType PropertyType { get; set; }
    public virtual bool IsBlittable => false;
    public virtual bool IsPlainOldData => false;
    public bool IsNetworkSupported = true;
    
    protected FieldDefinition? BackingField;
    
    public bool NeedsNativePropertyField { get; set; }
    
    private TypeReference? ToNativeDelegateType;
    private TypeReference? FromNativeDelegateType;
    // End non-json properties
    
    public NativeDataType(TypeReference typeRef, int arrayDim, PropertyType propertyType = PropertyType.Unknown)
    {
        if (typeRef.IsByReference)
        {
            typeRef = typeRef.GetElementType();
        }
        
        CSharpType = typeRef.ImportType();
        ArrayDim = arrayDim;
        PropertyType = propertyType;
    }

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
        
        BackingField = type.AddField($"{propertyMetaData.Name}_BackingField", CSharpType, FieldAttributes.Private);
    }
    
    public static Instruction[] GetArgumentBufferInstructions(Instruction? loadBufferInstruction, FieldDefinition offsetField)
    {
        List<Instruction> instructionBuffer = [];
        
        if (loadBufferInstruction != null)
        {
            instructionBuffer.Add(loadBufferInstruction);
        }
        else
        {
            instructionBuffer.Add(Instruction.Create(OpCodes.Ldarg_0));
            instructionBuffer.Add(Instruction.Create(OpCodes.Call, WeaverImporter.Instance.NativeObjectGetter));
        }

        instructionBuffer.Add(Instruction.Create(OpCodes.Ldsfld, offsetField));  
        instructionBuffer.Add(Instruction.Create(OpCodes.Call, WeaverImporter.Instance.IntPtrAdd));

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

    // Subclasses may override to do additional prep, such as adding additional backing fields.
    public virtual void PrepareForRewrite(TypeDefinition typeDefinition, PropertyMetaData propertyMetadata,
        object outer)
    {
        TypeReference? marshallingDelegates = WeaverImporter.Instance.UnrealSharpCoreAssembly.FindGenericType(WeaverImporter.UnrealSharpCoreMarshallers, "MarshallingDelegates`1", [CSharpType]);
        
        if (marshallingDelegates == null)
        {
            throw new Exception($"Could not find marshalling delegates for {CSharpType.FullName}");
        }
        
        TypeDefinition marshallingDelegatesDef = marshallingDelegates.Resolve();
        
        ToNativeDelegateType = marshallingDelegatesDef.FindNestedType("ToNative");
        FromNativeDelegateType = marshallingDelegatesDef.FindNestedType("FromNative");
    }

    protected void EmitDelegate(ILProcessor processor, TypeReference delegateType, MethodReference method)
    {
        processor.Emit(OpCodes.Ldnull);
        method = WeaverImporter.Instance.CurrentWeavingAssembly.MainModule.ImportReference(method);
        processor.Emit(OpCodes.Ldftn, method);
        MethodReference ctor = (from constructor in delegateType.Resolve().GetConstructors() where constructor.Parameters.Count == 2 select constructor).First().Resolve();
        ctor = FunctionProcessor.MakeMethodDeclaringTypeGeneric(ctor, CSharpType);
        ctor = WeaverImporter.Instance.CurrentWeavingAssembly.MainModule.ImportReference(ctor);
        processor.Emit(OpCodes.Newobj, ctor);
    }

    // Emits IL for a default constructible and possibly generic fixed array marshalling helper object.
    // If typeParams is null, a non-generic type is assumed.
    protected void EmitSimpleMarshallerDelegates(ILProcessor processor, string marshallerTypeName, TypeReference[]? typeParams)
    {
        TypeReference? marshallerType = null;
        AssemblyDefinition? marshallerAssembly = null;
        AssemblyUtilities.ForEachAssembly(action: assembly =>
        {
            if (typeParams is { Length: > 0 })
            {
                marshallerType = assembly.FindGenericType(string.Empty, marshallerTypeName, typeParams, false);
            }
            else
            {
                marshallerType = assembly.FindType(marshallerTypeName, string.Empty, false);
            }
            
            if (marshallerType != null)
            {
                marshallerAssembly = assembly;
            }
            
            return marshallerType == null;
        });
        
        if (marshallerType == null || marshallerAssembly == null)
        {
            throw new Exception($"Could not find marshaller type {marshallerTypeName} in any assembly.");
        }

        TypeDefinition marshallerTypeDef = GetMarshallerTypeDefinition(marshallerAssembly, marshallerType);
        MethodReference fromNative = marshallerTypeDef.FindMethod("FromNative")!;
        MethodReference toNative = marshallerTypeDef.FindMethod("ToNative")!;

        if (typeParams != null)
        {
            fromNative = FunctionProcessor.MakeMethodDeclaringTypeGeneric(fromNative, typeParams);
            toNative = FunctionProcessor.MakeMethodDeclaringTypeGeneric(toNative, typeParams);
        }
        
        if (ToNativeDelegateType == null || FromNativeDelegateType == null)
        {
            throw new Exception($"Could not find marshaller delegates for {marshallerTypeName}");
        }

        EmitDelegate(processor, ToNativeDelegateType, toNative);
        EmitDelegate(processor, FromNativeDelegateType, fromNative);
    }

    public abstract void EmitFixedArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type);
    public virtual void EmitDynamicArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type)
    {
        EmitFixedArrayMarshallerDelegates(processor, type);
    }

    public abstract void WriteGetter(TypeDefinition type, MethodDefinition getter, Instruction[] loadBufferPtr,
        FieldDefinition? fieldDefinition);
    public abstract void WriteSetter(TypeDefinition type, MethodDefinition setter, Instruction[] loadBufferPtr,
        FieldDefinition? fieldDefinition);

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

    protected static TypeDefinition GetMarshallerTypeDefinition(AssemblyDefinition assembly, TypeReference marshallerTypeReference)
    {
        return assembly.Modules.SelectMany(x => x.GetTypes()).First(x => x.Name == marshallerTypeReference.Name);
    }
    
}

