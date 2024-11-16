using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace UnrealSharpWeaver.NativeTypes;

public abstract class NativeDataSimpleType(TypeReference typeRef, string marshallerName, int arrayDim, PropertyType propertyType) 
    : NativeDataType(typeRef, arrayDim, propertyType)
{
    protected TypeReference MarshallerClass;
    protected MethodReference ToNative;
    protected MethodReference FromNative;
    
    private bool _isReference;
    private AssemblyDefinition? _assembly;
    
    public override bool IsPlainOldData => true;

    protected virtual TypeReference[] GetTypeParams()
    {
        return [WeaverHelper.ImportType(CSharpType)];
    }

    public override void PrepareForRewrite(TypeDefinition typeDefinition, FunctionMetaData? functionMetadata, PropertyMetaData propertyMetadata)
    {
        base.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);
        _isReference = propertyMetadata.IsOutParameter;
        _assembly = WeaverHelper.BindingsAssembly;
        var isGenericMarshaller = marshallerName.Contains('`');

        TypeReference[] typeParams = GetTypeParams();
        
        MarshallerClass = isGenericMarshaller
            ? WeaverHelper.FindGenericTypeInAssembly(_assembly, WeaverHelper.UnrealSharpNamespace, marshallerName, typeParams) 
            : GetTypeInAssembly();

        var marshallerTypeDefinition = GetMarshallerTypeDefinition();
        ToNative = WeaverHelper.FindMethod(marshallerTypeDefinition, "ToNative")!;
        FromNative = WeaverHelper.FindMethod(marshallerTypeDefinition, "FromNative")!;
        
        if (isGenericMarshaller)
        {
            ToNative = FunctionProcessor.MakeMethodDeclaringTypeGeneric(ToNative, typeParams);
            FromNative = FunctionProcessor.MakeMethodDeclaringTypeGeneric(FromNative, typeParams);
        }
        
        ToNative = WeaverHelper.ImportMethod(ToNative);
        FromNative = WeaverHelper.ImportMethod(FromNative);
    }

    protected override void CreateGetter(TypeDefinition type, MethodDefinition getter, FieldDefinition offsetField, FieldDefinition nativePropertyField)
    {
        ILProcessor processor = BeginSimpleGetter(getter);
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, null, offsetField);
        WriteMarshalFromNative(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0));
        EndSimpleGetter(processor, getter);
    }

    protected override void CreateSetter(TypeDefinition type, MethodDefinition setter, FieldDefinition offsetField, FieldDefinition nativePropertyField)
    {
        ILProcessor processor = BeginSimpleSetter(setter);
        Instruction loadValue = processor.Create(_isReference ? OpCodes.Ldarga : OpCodes.Ldarg, 1);
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, null, offsetField);
        WriteMarshalToNative(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), loadValue);
        EndSimpleSetter(processor, setter);
    }

    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBuffer, FieldDefinition offsetField, VariableDefinition localVar)
    {
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBuffer, offsetField);
        WriteMarshalFromNative(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0));
        processor.Emit(OpCodes.Stloc, localVar);
    }

    public override void WriteLoad(ILProcessor processor, TypeDefinition type, Instruction loadBuffer, FieldDefinition offsetField, FieldDefinition destField)
    {
        processor.Emit(OpCodes.Ldarg_0);
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBuffer, offsetField);
        WriteMarshalFromNative(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0));
        processor.Emit(OpCodes.Stfld, destField);
    }

    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type, Instruction loadBuffer, FieldDefinition offsetField, int argIndex, ParameterDefinition paramDefinition)
    {
        // Load parameter index onto the stack. First argument is always 1, because 0 is the instance.
        List<Instruction> source = [processor.Create(OpCodes.Ldarg, argIndex)];
        
        if (_isReference)
        {
            Instruction loadInstructionOutParam = WeaverHelper.CreateLoadInstructionOutParam(paramDefinition, propertyType);
            source.Add(loadInstructionOutParam);
        }
        
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBuffer, offsetField);
        
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), source.ToArray());
    }

    public override IList<Instruction>? WriteStore(ILProcessor processor, TypeDefinition type, Instruction loadBuffer, FieldDefinition offsetField, FieldDefinition srcField)
    {
        Instruction[] loadField =
        [
            processor.Create(OpCodes.Ldarg_0),
            processor.Create(_isReference ? OpCodes.Ldflda : OpCodes.Ldfld, srcField)
        ];
        
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, loadBuffer, offsetField);
        return WriteMarshalToNativeWithCleanup(processor, type, loadBufferInstructions, processor.Create(OpCodes.Ldc_I4_0), loadField);
    }
    
    public override void WriteMarshalToNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr, Instruction loadArrayIndex, Instruction[] loadSource)
    {
        foreach (var i in loadBufferPtr)
        {
            processor.Append(i);
        }
        
        processor.Append(loadArrayIndex);
        
        foreach (Instruction i in loadSource)
        {
            processor.Append(i);
        }
        
        processor.Emit(OpCodes.Call, ToNative);
    }

    public override void WriteMarshalFromNative(ILProcessor processor, TypeDefinition type, Instruction[] loadBufferPtr,
        Instruction loadArrayIndex)
    {
        foreach (var i in loadBufferPtr)
        {
            processor.Append(i);
        }
        
        processor.Append(loadArrayIndex);
        processor.Emit(OpCodes.Call, FromNative);
    }

    public override void EmitFixedArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type)
    {
        TypeReference[] typeParams = [];
        
        if (marshallerName.EndsWith("`1"))
        {
            if (!CSharpType.IsGenericInstance)
            {
                typeParams = [CSharpType];
            }
            else
            {
                GenericInstanceType generic = (GenericInstanceType)CSharpType;
                typeParams = [WeaverHelper.UserAssembly.MainModule.ImportReference(generic.GenericArguments[0].Resolve())];
            }
        }

        EmitSimpleMarshallerDelegates(processor, marshallerName, typeParams);
    }

    private TypeReference GetTypeInAssembly()
    {
        // Try to find the marshaller in the bindings assembly
        var typeInBindingAssembly = WeaverHelper.FindTypeInAssembly(_assembly, marshallerName, WeaverHelper.UnrealSharpNamespace, false);
        if (typeInBindingAssembly is not null)
        {
            return typeInBindingAssembly;
        }
        
        var propType = CSharpType.Resolve();
            
        // Try to find the marshaller in the bindings again, but with the namespace of the property type.
        var type = WeaverHelper.FindTypeInAssembly(_assembly, marshallerName, propType.Namespace, false);
        if (type is not null)
        {
            return type;
        }

        // Finally, try to find the marshaller in the user assembly.
        
        _assembly = GetUserAssemblyByPropertyType(propType);
        return WeaverHelper.FindTypeInAssembly(_assembly, marshallerName, propType.Namespace)!;
    }

    private AssemblyDefinition GetUserAssemblyByPropertyType(TypeDefinition type)
    {
        var propertyTypeName = type.Module.Assembly.FullName;
        return WeaverHelper.UserAssembly.FullName == propertyTypeName
            ? WeaverHelper.UserAssembly
            : WeaverHelper.WeavedAssemblies.First(x => x.FullName == propertyTypeName);
    }

    private TypeDefinition GetMarshallerTypeDefinition()
    {
        return GetMarshallerTypeDefinition(_assembly, MarshallerClass);
    }
}