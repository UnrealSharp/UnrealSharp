using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.TypeProcessors;

public static class ConstructorBuilder
{
    public static MethodDefinition MakeStaticConstructor(TypeDefinition type)
    {
        return CreateStaticConstructor(type, MethodAttributes.Static);
    }

    public static MethodDefinition CreateStaticConstructor(TypeDefinition type, MethodAttributes attributes, params TypeReference[] parameterTypes)
    {
        MethodDefinition staticConstructor = type.GetStaticConstructor();

        if (staticConstructor == null)
        {
            staticConstructor = type.AddMethod(".cctor", 
                WeaverImporter.Instance.VoidTypeRef,
                attributes | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
                parameterTypes);
        }
        else
        {
            // Remove the return instruction from existing static constructor
            staticConstructor.RemoveReturnInstruction();
        }
        
        return staticConstructor;
    }
    
    public static MethodDefinition CreateConstructor(TypeDefinition typeDefinition, MethodAttributes attributes, params TypeReference[] parameterTypes)
    {
        MethodDefinition? constructor = typeDefinition.GetConstructors().FirstOrDefault(ctor => ctor.Parameters.Count == parameterTypes.Length);
        
        if (constructor == null)
        {
            constructor = typeDefinition.AddMethod(".ctor", 
                WeaverImporter.Instance.VoidTypeRef,
                attributes | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                parameterTypes);
        }
        
        return constructor;
    }
    
    public static void CreateTypeInitializer(TypeDefinition typeDefinition, Instruction field, Instruction[] initializeInstructions, string engineName = "", bool finalizeMethod = false)
    {
        MethodDefinition staticConstructorMethod = MakeStaticConstructor(typeDefinition);
        ILProcessor processor = staticConstructorMethod.Body.GetILProcessor();
        
        processor.Emit(OpCodes.Ldstr, typeDefinition.Module.Assembly.Name.Name);
        processor.Emit(OpCodes.Ldstr, typeDefinition.Namespace);
        
        engineName = string.IsNullOrEmpty(engineName) ? typeDefinition.GetEngineName() : engineName;
        processor.Emit(OpCodes.Ldstr, engineName);
        
        foreach (Instruction instruction in initializeInstructions)
        {
            processor.Append(instruction);
        }
        
        processor.Append(field);
        
        if (finalizeMethod)
        {
            staticConstructorMethod.FinalizeMethod();
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

    public static void InitializeFields(MethodDefinition staticConstructor, List<PropertyMetaData> fields, Instruction loadNativeClassField)
    {
        ILProcessor processor = staticConstructor.Body.GetILProcessor();
        foreach (var property in fields)
        {
            if (property.HasCustomAccessors) 
            {
                continue;
            }
            
            Instruction loadNativeProperty;
            Instruction setNativeProperty;
            if (property.NativePropertyField == null)
            {
                VariableDefinition nativePropertyVar = processor.Body.Method.AddLocalVariable(WeaverImporter.Instance.IntPtrType);
                loadNativeProperty = Instruction.Create(OpCodes.Ldloc, nativePropertyVar);
                setNativeProperty = Instruction.Create(OpCodes.Stloc, nativePropertyVar);
            }
            else
            {
                loadNativeProperty = Instruction.Create(OpCodes.Ldsfld, property.NativePropertyField);
                setNativeProperty = Instruction.Create(OpCodes.Stsfld, property.NativePropertyField);
            }
            
            property.InitializePropertyPointers(processor, loadNativeClassField, setNativeProperty);
            property.InitializePropertyOffsets(processor, loadNativeProperty);
            property.PropertyDataType.WritePostInitialization(processor, property, loadNativeProperty, setNativeProperty);
        }
    }
}