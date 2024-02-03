using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.Rewriters;

public static class ConstructorBuilder
{
    public static MethodDefinition MakeStaticConstructor(TypeDefinition type)
    {
        return CreateConstructor(type, MethodAttributes.Static);
    }

    public static MethodDefinition CreateConstructor(TypeDefinition type, MethodAttributes attributes, params TypeReference[] parameterTypes)
    {
        MethodDefinition staticConstructor = type.GetStaticConstructor();

        if (staticConstructor != null)
        {
            return staticConstructor;
        }
        
        staticConstructor = WeaverHelper.AddMethodToType(type, ".cctor", 
            WeaverHelper.VoidTypeRef,
            attributes | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
            parameterTypes);

        return staticConstructor;
    }
    
    public static void CreateTypeInitializer(TypeDefinition typeDefinition, Instruction field, Instruction[] initializeInstructions)
    {
        MethodDefinition staticConstructorMethod = MakeStaticConstructor(typeDefinition);
        ILProcessor processor = staticConstructorMethod.Body.GetILProcessor();
        
        processor.Emit(OpCodes.Ldstr, typeDefinition.Name);
        
        foreach (Instruction instruction in initializeInstructions)
        {
            processor.Append(instruction);
        }
        
        processor.Append(field);
    }
    
    public static void InitializePropertyPointers(ILProcessor processor, Instruction loadNativeType, 
        List<Tuple<FieldDefinition, PropertyMetaData>>? propertyPointersToInitialize)
    {
        if (propertyPointersToInitialize == null)
        {
            return;
        }
        
        foreach (var nativeProp in propertyPointersToInitialize)
        {
            processor.Append(loadNativeType);
            processor.Emit(OpCodes.Ldstr, nativeProp.Item2.Name);
            processor.Emit(OpCodes.Call, WeaverHelper.GetNativePropertyFromNameMethod);
            processor.Emit(OpCodes.Stsfld, nativeProp.Item1);
        }
    }
    
    public static void InitializePropertyOffsets(ILProcessor processor, Instruction loadNativeType, 
        List<Tuple<FieldDefinition, PropertyMetaData>>? propertyOffsetsToInitialize)
    {
        if (propertyOffsetsToInitialize == null)
        {
            return;
        }
        
        foreach (var offset in propertyOffsetsToInitialize)
        {
            processor.Append(loadNativeType);
            processor.Emit(OpCodes.Ldstr, offset.Item2.Name);
            processor.Emit(OpCodes.Call, WeaverHelper.GetPropertyOffsetFromNameMethod);
            processor.Emit(OpCodes.Stsfld, offset.Item1);
        }
    }
    
    public static void VerifySingleResult<T>(T[] results, TypeDefinition type, string endMessage)
    {
        switch (results.Length)
        {
            case 0:
                throw new RewriteException(type, $"Could not find {endMessage}");
            case > 1:
                throw new RewriteException(type, $"Found more than one {endMessage}");
        }
    }
}