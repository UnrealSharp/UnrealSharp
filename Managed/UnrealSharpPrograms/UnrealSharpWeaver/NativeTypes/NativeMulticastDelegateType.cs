using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Rewriters;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataMulticastDelegate : NativeDataSimpleType
{
    public FunctionMetaData Signature { get; set; }
    
    public NativeDataMulticastDelegate(TypeReference delegateType, string unrealClass, int arrayDim) 
        : base(delegateType, "DelegateMarshaller`1", unrealClass, arrayDim, PropertyType.Delegate)
    {
        NeedsNativePropertyField = true;
        
        foreach (TypeDefinition nestedType in delegateType.Resolve().NestedTypes)
        {
            foreach (MethodDefinition method in nestedType.Methods)
            {
                if (method.Name != "Invoke")
                {
                    continue;
                }

                Signature = new FunctionMetaData(method)
                {
                    // Don't give a name to the delegate function, it'll cause a name collision with other delegates in the same class.
                    // Let Unreal Engine handle the name generation.
                    Name = "",
                    FunctionFlags = FunctionFlags.Delegate | FunctionFlags.MulticastDelegate
                };

                return;
            }
        }
    }

    public override void PrepareForRewrite(TypeDefinition typeDefinition, FunctionMetaData? functionMetadata, PropertyMetaData propertyMetadata)
    {
        AddBackingField(typeDefinition, propertyMetadata);
        base.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);
    }

    public override void WritePostInitialization(ILProcessor processor, PropertyMetaData propertyMetadata, VariableDefinition propertyPointer)
    {
        if (Signature.Parameters.Length == 0)
        {
            return;
        }
        
        PropertyDefinition propertyRef = (PropertyDefinition) propertyMetadata.MemberRef.Resolve();
        MethodReference? Initialize = WeaverHelper.FindMethod(propertyRef.PropertyType.Resolve(), UnrealDelegateProcessor.InitializeUnrealDelegate);
        processor.Emit(OpCodes.Ldloc, propertyPointer);
        processor.Emit(OpCodes.Call, Initialize);
    }

    protected override void CreateGetter(TypeDefinition type, MethodDefinition getter, FieldDefinition offsetField, FieldDefinition nativePropertyField)
    {
        ILProcessor processor = BeginSimpleGetter(getter);
        Instruction loadOwner = processor.Create(OpCodes.Ldarg_0);
        
        // Make an if-statement to check if the backing field is valid.
        // if (MyDelegate_BackingField == null)
        // {
        // }
        {
            processor.Append(loadOwner);
            processor.Emit(OpCodes.Ldfld, nativePropertyField);
            processor.Append(processor.Create(OpCodes.Brfalse, processor.Create(OpCodes.Ldarg_0)));
        }
        
        Instruction[] loadBufferInstructions = GetArgumentBufferInstructions(processor, null, offsetField);
        List<Instruction> allCleanupInstructions = loadBufferInstructions.ToList();
        allCleanupInstructions.Add(Instruction.Create(OpCodes.Ldsfld, nativePropertyField));
        
        WriteMarshalFromNative(processor, type, allCleanupInstructions.ToArray(), processor.Create(OpCodes.Ldc_I4_0), loadOwner);
        EndSimpleGetter(processor, getter);
    }
}